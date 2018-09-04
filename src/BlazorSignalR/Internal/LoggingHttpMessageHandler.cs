using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace BlazorSignalR.Internal
{
    internal class LoggingHttpMessageHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHttpMessageHandler> _logger;

        public LoggingHttpMessageHandler(HttpMessageHandler inner, ILoggerFactory loggerFactory)
            : base(inner)
        {
            if (loggerFactory == null)
                throw new ArgumentNullException(nameof(loggerFactory));
            this._logger = loggerFactory.CreateLogger<LoggingHttpMessageHandler>();
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Log.SendingHttpRequest(_logger, request.Method, request.RequestUri);
            HttpResponseMessage httpResponseMessage = await base.SendAsync(request, cancellationToken);
            if (!httpResponseMessage.IsSuccessStatusCode)
                Log.UnsuccessfulHttpResponse(_logger, httpResponseMessage.StatusCode, request.Method,
                    request.RequestUri);
            return httpResponseMessage;
        }

        private static class Log
        {
            private static readonly Action<ILogger, HttpMethod, Uri, Exception> _sendingHttpRequest =
                LoggerMessage.Define<HttpMethod, Uri>(LogLevel.Trace, new EventId(1, "SendingHttpRequest"),
                    "Sending HTTP request {RequestMethod} '{RequestUrl}'.");

            private static readonly Action<ILogger, int, HttpMethod, Uri, Exception> _unsuccessfulHttpResponse =
                LoggerMessage.Define<int, HttpMethod, Uri>(LogLevel.Warning, new EventId(2, "UnsuccessfulHttpResponse"),
                    "Unsuccessful HTTP response {StatusCode} return from {RequestMethod} '{RequestUrl}'.");

            public static void SendingHttpRequest(ILogger logger, HttpMethod requestMethod, Uri requestUrl)
            {
                Log._sendingHttpRequest(logger, requestMethod, requestUrl, (Exception) null);
            }

            public static void UnsuccessfulHttpResponse(ILogger logger, HttpStatusCode statusCode,
                HttpMethod requestMethod, Uri requestUrl)
            {
                Log._unsuccessfulHttpResponse(logger, (int) statusCode, requestMethod, requestUrl, (Exception) null);
            }
        }
    }
}