using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace BlazorSignalR.Internal
{
    internal class BlazorHttpConnectionFactory : IConnectionFactory
    {
        private readonly BlazorHttpConnectionOptions _options;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILoggerFactory _loggerFactory;
        private readonly NavigationManager _navigationManager;

        public BlazorHttpConnectionFactory(IOptions<BlazorHttpConnectionOptions> options, IJSRuntime jsRuntime, ILoggerFactory loggerFactory, NavigationManager navigationManager)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            _options = options.Value;
            _jsRuntime = jsRuntime;
            _loggerFactory = loggerFactory;
            _navigationManager = navigationManager;
        }

        public async ValueTask<ConnectionContext> ConnectAsync(EndPoint endPoint, CancellationToken cancellationToken = default)
        {
            if (endPoint == null)
            {
                throw new ArgumentNullException(nameof(endPoint));
            }

            if (!(endPoint is UriEndPoint uriEndPoint))
            {
                throw new NotSupportedException($"The provided {nameof(EndPoint)} must be of type {nameof(UriEndPoint)}.");
            }

            if (_options.Url != null && _options.Url != uriEndPoint.Uri)
            {
                throw new InvalidOperationException($"If {nameof(BlazorHttpConnectionOptions)}.{nameof(BlazorHttpConnectionOptions.Url)} was set, it must match the {nameof(UriEndPoint)}.{nameof(UriEndPoint.Uri)} passed to {nameof(ConnectAsync)}.");
            }

            var shallowCopiedOptions = ShallowCopyHttpConnectionOptions(_options);
            shallowCopiedOptions.Url = uriEndPoint.Uri;

            var connection = new BlazorHttpConnection(shallowCopiedOptions, _jsRuntime, _loggerFactory, _navigationManager);
            
            try
            {
                await connection.StartAsync();
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        // Internal for testing
        internal static BlazorHttpConnectionOptions ShallowCopyHttpConnectionOptions(BlazorHttpConnectionOptions options)
        {
            return new BlazorHttpConnectionOptions
            {
                HttpMessageHandlerFactory = options.HttpMessageHandlerFactory,
                Headers = options.Headers,
                //ClientCertificates = options.ClientCertificates,
                //Cookies = options.Cookies,
                Url = options.Url,
                Transports = options.Transports,
                SkipNegotiation = options.SkipNegotiation,
                AccessTokenProvider = options.AccessTokenProvider,
                //CloseTimeout = options.CloseTimeout,
                //Credentials = options.Credentials,
                //Proxy = options.Proxy,
                //UseDefaultCredentials = options.UseDefaultCredentials,
                DefaultTransferFormat = options.DefaultTransferFormat,
                //WebSocketConfiguration = options.WebSocketConfiguration,
            };
        }
    }
}