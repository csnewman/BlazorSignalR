using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace BlazorSignalR.Internal
{
    public class BlazorServerSentEventsTransport : ITransport
    {
        private readonly HttpClient _httpClient;
        private readonly IJSInProcessRuntime _jsRuntime;
        private readonly ILogger _logger;
        private volatile Exception _error;
        private readonly CancellationTokenSource _transportCts = new CancellationTokenSource();

        private IDuplexPipe _transport;
        private IDuplexPipe _application;

        public string InternalSSEId { [JSInvokable] get; }

        public string SSEAccessToken { [JSInvokable] get; }

        internal Task Running { get; private set; } = Task.CompletedTask;

        public PipeReader Input => _transport.Input;

        public PipeWriter Output => _transport.Output;

        private TaskCompletionSource<object> _jsTask;

        public BlazorServerSentEventsTransport(string token, HttpClient httpClient, IJSInProcessRuntime jsRuntime, ILoggerFactory loggerFactory)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            _httpClient = httpClient;
            _jsRuntime = jsRuntime;
            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<BlazorServerSentEventsTransport>();
            InternalSSEId = Guid.NewGuid().ToString();
            SSEAccessToken = token;
        }

        public Task StartAsync(Uri url, TransferFormat transferFormat)
        {
            if (transferFormat != TransferFormat.Text)
            {
                throw new ArgumentException(
                    $"The '{transferFormat}' transfer format is not supported by this transport.",
                    nameof(transferFormat));
            }

            Log.StartTransport(_logger, transferFormat);

            // Create pipe
            PipeOptions options = ClientPipeOptions.DefaultOptions;
            DuplexPipe.DuplexPipePair pair = DuplexPipe.CreateConnectionPair(options, options);

            _transport = pair.Transport;
            _application = pair.Application;

            CancellationTokenSource inputCts = new CancellationTokenSource();
            _application.Input.OnWriterCompleted((exception, state) => ((CancellationTokenSource)state).Cancel(),
                inputCts);

            // Start streams
            Running = ProcessAsync(url, inputCts.Token);

            return Task.CompletedTask;
        }

        private async Task ProcessAsync(Uri url, CancellationToken inputCancellationToken)
        {
            // Start sending and receiving
            Task receiving = ProcessEventStream(url.ToString(), _application, _transportCts.Token);
            Task sending = SendUtils.SendMessages(url, _application, _httpClient, _logger, inputCancellationToken);

            // Wait for send or receive to complete
            Task trigger = await Task.WhenAny(receiving, sending);

            if (trigger == receiving)
            {
                // Cancel the application so that ReadAsync yields
                _application.Input.CancelPendingRead();

                await sending;
            }
            else
            {
                // Set the sending error so we communicate that to the application
                _error = sending.IsFaulted ? sending.Exception.InnerException : null;

                _transportCts.Cancel();

                // Cancel any pending flush so that we can quit
                _application.Output.CancelPendingFlush();

                await receiving;
            }
        }

        private async Task ProcessEventStream(string url, IDuplexPipe application, CancellationToken transportCtsToken)
        {
            Log.StartReceive(_logger);

            try
            {
                // Creates a task to represent the SSE js processing
                TaskCompletionSource<object> task = new TaskCompletionSource<object>();
                _jsTask = task;

                // Create connection
                _jsRuntime.Invoke<object>(
                    "BlazorSignalR.ServerSentEventsTransport.CreateConnection", url, new DotNetObjectRef(this));

                // If canceled, stop fake processing
                transportCtsToken.Register(() => { task.SetCanceled(); });

                // Wait until js side stops
                await task.Task;

                if (task.Task.IsCanceled)
                {
                    Log.ReceiveCanceled(_logger);
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug($"SSE JS Side error {ex.Message}");
                _error = ex;
            }
            finally
            {
                _application.Output.Complete(_error);

                Log.ReceiveStopped(_logger);

                // Close JS side SSE
                CloseSSE();
            }
        }

        [JSInvokable]
        public void HandleSSEMessage(string msg)
        {
            _logger.LogDebug($"HandleSSEMessage \"{msg}\"");

            // Decode data
            Log.ParsingSSE(_logger, msg.Length);

            byte[] data = Convert.FromBase64String(msg);
            Log.MessageToApplication(_logger, data.Length);

            // Write to stream
            FlushResult flushResult = _application.Output.WriteAsync(data).Result;

            // Handle cancel
            if (flushResult.IsCanceled || flushResult.IsCompleted)
            {
                Log.EventStreamEnded(_logger);

                _jsTask.SetCanceled();
            }
        }

        [JSInvokable]
        public void HandleSSEError(string msg)
        {
            _logger.LogDebug($"HandleSSEError \"{msg}\"");
            _jsTask.SetException(new Exception(msg));
        }

        [JSInvokable]
        public void HandleSSEOpened()
        {
            _logger.LogDebug("HandleSSEOpened");
        }

        public async Task StopAsync()
        {
            Log.TransportStopping(_logger);

            if (_application == null)
            {
                // We never started
                return;
            }

            // Kill js side
            _jsTask.SetCanceled();
            CloseSSE();

            // Cleanup managed side
            _transport.Output.Complete();
            _transport.Input.Complete();

            _application.Input.CancelPendingRead();

            try
            {
                await Running;
            }
            catch (Exception ex)
            {
                Log.TransportStopped(_logger, ex);
                throw;
            }

            Log.TransportStopped(_logger, null);
        }

        public void CloseSSE()
        {
            try
            {
                _jsRuntime.Invoke<object>(
                    "BlazorSignalR.ServerSentEventsTransport.CloseConnection", new DotNetObjectRef(this));
            }
            catch (Exception e)
            {
                _logger.LogError($"Failed to stop SSE {e}");
            }
        }

        public static bool IsSupported(IJSInProcessRuntime jsRuntime)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            return jsRuntime.Invoke<bool>(
                "BlazorSignalR.ServerSentEventsTransport.IsSupported");
        }

        private static class Log
        {
            private static readonly Action<ILogger, TransferFormat, Exception> _startTransport =
                LoggerMessage.Define<TransferFormat>(LogLevel.Information, new EventId(1, "StartTransport"),
                    "Starting transport. Transfer mode: {TransferFormat}.");

            private static readonly Action<ILogger, Exception> _transportStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(2, "TransportStopped"), "Transport stopped.");

            private static readonly Action<ILogger, Exception> _startReceive =
                LoggerMessage.Define(LogLevel.Debug, new EventId(3, "StartReceive"), "Starting receive loop.");

            private static readonly Action<ILogger, Exception> _receiveStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(4, "ReceiveStopped"), "Receive loop stopped.");

            private static readonly Action<ILogger, Exception> _receiveCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(5, "ReceiveCanceled"), "Receive loop canceled.");

            private static readonly Action<ILogger, Exception> _transportStopping =
                LoggerMessage.Define(LogLevel.Information, new EventId(6, "TransportStopping"),
                    "Transport is stopping.");

            private static readonly Action<ILogger, int, Exception> _messageToApplication =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(7, "MessageToApplication"),
                    "Passing message to application. Payload size: {Count}.");

            private static readonly Action<ILogger, Exception> _eventStreamEnded =
                LoggerMessage.Define(LogLevel.Debug, new EventId(8, "EventStreamEnded"),
                    "Server-Sent Event Stream ended.");

            private static readonly Action<ILogger, long, Exception> _parsingSSE =
                LoggerMessage.Define<long>(LogLevel.Debug, new EventId(9, "ParsingSSE"),
                    "Received {Count} bytes. Parsing SSE frame.");

            public static void StartTransport(ILogger logger, TransferFormat transferFormat)
            {
                _startTransport(logger, transferFormat, null);
            }

            public static void TransportStopped(ILogger logger, Exception exception)
            {
                _transportStopped(logger, exception);
            }

            public static void StartReceive(ILogger logger)
            {
                _startReceive(logger, null);
            }

            public static void TransportStopping(ILogger logger)
            {
                _transportStopping(logger, null);
            }

            public static void MessageToApplication(ILogger logger, int count)
            {
                _messageToApplication(logger, count, null);
            }

            public static void ReceiveCanceled(ILogger logger)
            {
                _receiveCanceled(logger, null);
            }

            public static void ReceiveStopped(ILogger logger)
            {
                _receiveStopped(logger, null);
            }

            public static void EventStreamEnded(ILogger logger)
            {
                _eventStreamEnded(logger, null);
            }

            public static void ParsingSSE(ILogger logger, long bytes)
            {
                _parsingSSE(logger, bytes, null);
            }
        }
    }
}