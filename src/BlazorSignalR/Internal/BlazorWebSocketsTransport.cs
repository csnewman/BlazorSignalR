using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections.Client.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.JSInterop;

namespace BlazorSignalR.Internal
{
    public class BlazorWebSocketsTransport : IDuplexPipe
    {
        private IDuplexPipe _application;
        private readonly ILogger _logger;
        private readonly IJSRuntime _jsRuntime;
        private volatile bool _aborted;

        private IDuplexPipe _transport;

        public string InternalWebSocketId { [JSInvokable] get; }

        public string WebSocketAccessToken { [JSInvokable] get; }

        internal Task Running { get; private set; } = Task.CompletedTask;

        public PipeReader Input => _transport.Input;

        public PipeWriter Output => _transport.Output;

        private TaskCompletionSource<object> _startTask;
        private TaskCompletionSource<object> _receiveTask;

        public BlazorWebSocketsTransport(string token, IJSRuntime jsRuntime, ILoggerFactory loggerFactory)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            _logger = (loggerFactory ?? NullLoggerFactory.Instance).CreateLogger<BlazorWebSocketsTransport>();
            InternalWebSocketId = Guid.NewGuid().ToString();
            WebSocketAccessToken = token;
            _jsRuntime = jsRuntime;
        }

        public async Task StartAsync(Uri url, TransferFormat transferFormat, CancellationToken cancellationToken)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (transferFormat != TransferFormat.Binary && transferFormat != TransferFormat.Text)
            {
                throw new ArgumentException(
                    $"The '{transferFormat}' transfer format is not supported by this transport.",
                    nameof(transferFormat));
            }


            Log.StartTransport(_logger, transferFormat);

            // Create connection
            _startTask = new TaskCompletionSource<object>();
            await _jsRuntime.InvokeAsync<object>(
                "BlazorSignalR.WebSocketsTransport.CreateConnection", url.ToString(),
                transferFormat == TransferFormat.Binary, DotNetObjectRef.Create(this));

            await _startTask.Task;
            _startTask = null;

            Log.StartedTransport(_logger);

            // Create the pipe pair (Application's writer is connected to Transport's reader, and vice versa)
            PipeOptions options = ClientPipeOptions.DefaultOptions;
            DuplexPipe.DuplexPipePair pair = DuplexPipe.CreateConnectionPair(options, options);

            _transport = pair.Transport;
            _application = pair.Application;

            Running = ProcessSocketAsync();
        }

        private async Task ProcessSocketAsync()
        {
            // Begin sending and receiving.
            var receiving = StartReceiving();
            var sending = StartSending();

            // Wait for send or receive to complete
            var trigger = await Task.WhenAny(receiving, sending);

            if (trigger == receiving)
            {
                // We're waiting for the application to finish and there are 2 things it could be doing
                // 1. Waiting for application data
                // 2. Waiting for a websocket send to complete

                // Cancel the application so that ReadAsync yields
                _application.Input.CancelPendingRead();

                using (CancellationTokenSource delayCts = new CancellationTokenSource())
                {
                    Task resultTask =
                        await Task.WhenAny(sending, Task.Delay(TimeSpan.FromSeconds(5.0), delayCts.Token));

                    if (resultTask != sending)
                    {
                        _aborted = true;

                        // Abort the websocket if we're stuck in a pending send to the client
                        _receiveTask.SetCanceled();
                        await CloseWebSocketAsync();
                    }
                    else
                    {
                        // Cancel the timeout
                        delayCts.Cancel();
                    }
                }
            }
            else
            {
                // We're waiting on the websocket to close and there are 2 things it could be doing
                // 1. Waiting for websocket data
                // 2. Waiting on a flush to complete (backpressure being applied)

                _aborted = true;

                // Abort the websocket if we're stuck in a pending receive from the client
                _receiveTask.SetCanceled();
                await CloseWebSocketAsync();

                // Cancel any pending flush so that we can quit
                _application.Output.CancelPendingFlush();
            }
        }

        private async Task StartReceiving()
        {
            try
            {
                TaskCompletionSource<object> task = new TaskCompletionSource<object>();
                _receiveTask = task;

                // Wait until js side stops
                await task.Task;
            }
            catch (OperationCanceledException)
            {
                Log.ReceiveCanceled(_logger);
            }
            catch (Exception ex)
            {
                if (!_aborted)
                {
                    _application.Output.Complete(ex);

                    // We re-throw here so we can communicate that there was an error when sending
                    // the close frame
                    throw;
                }
            }
            finally
            {
                // We're done writing
                _application.Output.Complete();

                Log.ReceiveStopped(_logger);
            }
        }

        [JSInvokable]
        public void HandleWebSocketMessage(string msg)
        {
            _logger.LogDebug($"HandleWebSocketMessage \"{msg}\"");

            // Decode data
            byte[] data = Convert.FromBase64String(msg);

            Log.MessageReceived(_logger, data.Length);

            // Write to stream
            FlushResult flushResult = _application.Output.WriteAsync(data).Result;

            // Handle cancel
            if (flushResult.IsCanceled || flushResult.IsCompleted)
            {
                _receiveTask.SetCanceled();
            }
        }

        private async Task StartSending()
        {
            Exception error = null;

            try
            {
                while (true)
                {
                    ReadResult result = await _application.Input.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;

                    // Get a frame from the application

                    try
                    {
                        if (result.IsCanceled)
                        {
                            break;
                        }

                        if (!buffer.IsEmpty)
                        {
                            try
                            {
                                Log.ReceivedFromApp(_logger, buffer.Length);

                                string data = Convert.ToBase64String(buffer.ToArray());

                                Log.SendStarted(_logger);

                                await _jsRuntime.InvokeAsync<object>(
                                    "BlazorSignalR.WebSocketsTransport.Send", data, DotNetObjectRef.Create(this));
                            }
                            catch (Exception ex)
                            {
                                if (!_aborted)
                                {
                                    Log.ErrorSendingMessage(_logger, ex);
                                }

                                break;
                            }
                        }
                        else if (result.IsCompleted)
                        {
                            break;
                        }
                    }
                    finally
                    {
                        _application.Input.AdvanceTo(buffer.End);
                    }
                }
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                await CloseWebSocketAsync();

                _application.Input.Complete();

                Log.SendStopped(_logger);
            }
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
            _startTask?.SetCanceled();
            _receiveTask?.SetCanceled();
            await CloseWebSocketAsync();

            _transport.Output.Complete();
            _transport.Input.Complete();

            // Cancel any pending reads from the application, this should start the entire shutdown process
            _application.Input.CancelPendingRead();

            try
            {
                await Running;
            }
            catch (Exception ex)
            {
                Log.TransportStopped(_logger, ex);
                // exceptions have been handled in the Running task continuation by closing the channel with the exception
                return;
            }

            Log.TransportStopped(_logger, null);
        }

        public async Task CloseWebSocketAsync()
        {
            Log.ClosingWebSocket(_logger);
            try
            {
                await _jsRuntime.InvokeAsync<object>(
                    "BlazorSignalR.WebSocketsTransport.CloseConnection", DotNetObjectRef.Create(this));
            }
            catch (Exception e)
            {
                Log.ClosingWebSocketFailed(_logger, e);
            }
        }

        [JSInvokable]
        public void HandleWebSocketError(string msg)
        {
            _logger.LogDebug($"HandleWebSocketError \"{msg}\"");
            _startTask?.SetException(new Exception(msg));
            _receiveTask?.SetException(new Exception(msg));
        }

        [JSInvokable]
        public void HandleWebSocketOpened()
        {
            _logger.LogDebug("HandleWebSocketOpened");
            _startTask.SetResult(null);
        }

        [JSInvokable]
        public void HandleWebSocketClosed()
        {
            _logger.LogDebug("HandleWebSocketClosed");
            _startTask?.SetCanceled();
            _receiveTask?.SetCanceled();
        }

        public static Task<bool> IsSupportedAsync(IJSRuntime jsRuntime)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            return jsRuntime.InvokeAsync<bool>(
                "BlazorSignalR.WebSocketsTransport.IsSupported");
        }

        private static class Log
        {
            private static readonly Action<ILogger, TransferFormat, Exception> _startTransport =
                LoggerMessage.Define<TransferFormat>(LogLevel.Information, new EventId(1, "StartTransport"),
                    "Starting transport. Transfer mode: {TransferFormat}. ");

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

            private static readonly Action<ILogger, Exception> _sendStarted =
                LoggerMessage.Define(LogLevel.Debug, new EventId(7, "SendStarted"), "Starting the send loop.");

            private static readonly Action<ILogger, Exception> _sendStopped =
                LoggerMessage.Define(LogLevel.Debug, new EventId(8, "SendStopped"), "Send loop stopped.");

            private static readonly Action<ILogger, Exception> _sendCanceled =
                LoggerMessage.Define(LogLevel.Debug, new EventId(9, "SendCanceled"), "Send loop canceled.");

            private static readonly Action<ILogger, int, Exception> _messageToApp =
                LoggerMessage.Define<int>(LogLevel.Debug, new EventId(10, "MessageToApp"),
                    "Passing message to application. Payload size: {Count}.");

            private static readonly Action<ILogger, WebSocketCloseStatus?, Exception> _webSocketClosed =
                LoggerMessage.Define<WebSocketCloseStatus?>(LogLevel.Information, new EventId(11, "WebSocketClosed"),
                    "WebSocket closed by the server. Close status {CloseStatus}.");

            private static readonly Action<ILogger, int, Exception> _messageReceived =
                LoggerMessage.Define<int>(LogLevel.Debug,
                    new EventId(12, "MessageReceived"),
                    "Message received.  size: {Count}.");

            private static readonly Action<ILogger, long, Exception> _receivedFromApp =
                LoggerMessage.Define<long>(LogLevel.Debug, new EventId(13, "ReceivedFromApp"),
                    "Received message from application. Payload size: {Count}.");

            private static readonly Action<ILogger, Exception> _sendMessageCanceled =
                LoggerMessage.Define(LogLevel.Information, new EventId(14, "SendMessageCanceled"),
                    "Sending a message canceled.");

            private static readonly Action<ILogger, Exception> _errorSendingMessage =
                LoggerMessage.Define(LogLevel.Error, new EventId(15, "ErrorSendingMessage"),
                    "Error while sending a message.");

            private static readonly Action<ILogger, Exception> _closingWebSocket =
                LoggerMessage.Define(LogLevel.Information, new EventId(16, "ClosingWebSocket"), "Closing WebSocket.");

            private static readonly Action<ILogger, Exception> _closingWebSocketFailed =
                LoggerMessage.Define(LogLevel.Information, new EventId(17, "ClosingWebSocketFailed"),
                    "Closing webSocket failed.");

            private static readonly Action<ILogger, Exception> _cancelMessage =
                LoggerMessage.Define(LogLevel.Debug, new EventId(18, "CancelMessage"),
                    "Canceled passing message to application.");

            private static readonly Action<ILogger, Exception> _startedTransport =
                LoggerMessage.Define(LogLevel.Debug, new EventId(19, "StartedTransport"), "Started transport.");

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

            public static void MessageToApp(ILogger logger, int count)
            {
                _messageToApp(logger, count, null);
            }

            public static void ReceiveCanceled(ILogger logger)
            {
                _receiveCanceled(logger, null);
            }

            public static void ReceiveStopped(ILogger logger)
            {
                _receiveStopped(logger, null);
            }

            public static void SendStarted(ILogger logger)
            {
                _sendStarted(logger, null);
            }

            public static void SendCanceled(ILogger logger)
            {
                _sendCanceled(logger, null);
            }

            public static void SendStopped(ILogger logger)
            {
                _sendStopped(logger, null);
            }

            public static void WebSocketClosed(ILogger logger, WebSocketCloseStatus? closeStatus)
            {
                _webSocketClosed(logger, closeStatus, null);
            }

            public static void MessageReceived(ILogger logger, int count)
            {
                _messageReceived(logger, count, null);
            }

            public static void ReceivedFromApp(ILogger logger, long count)
            {
                _receivedFromApp(logger, count, null);
            }

            public static void SendMessageCanceled(ILogger logger)
            {
                _sendMessageCanceled(logger, null);
            }

            public static void ErrorSendingMessage(ILogger logger, Exception exception)
            {
                _errorSendingMessage(logger, exception);
            }

            public static void ClosingWebSocket(ILogger logger)
            {
                _closingWebSocket(logger, null);
            }

            public static void ClosingWebSocketFailed(ILogger logger, Exception exception)
            {
                _closingWebSocketFailed(logger, exception);
            }

            public static void CancelMessage(ILogger logger)
            {
                _cancelMessage(logger, null);
            }

            public static void StartedTransport(ILogger logger)
            {
                _startedTransport(logger, null);
            }
        }
    }
}