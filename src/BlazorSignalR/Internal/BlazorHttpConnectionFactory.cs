using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Services;
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
        private readonly IUriHelper _uriHelper;
        private readonly bool _isServerSide;
        private readonly ILoggerFactory _loggerFactory;

        public BlazorHttpConnectionFactory(
            IOptions<BlazorHttpConnectionOptions> options,
            IJSRuntime jsRuntime,
            IUriHelper uriHelper,
            ILoggerFactory loggerFactory)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            if (uriHelper == null)
                throw new ArgumentNullException(nameof(uriHelper));

            _options = options.Value;
            _jsRuntime = jsRuntime;
            _uriHelper = uriHelper;
            _loggerFactory = loggerFactory;

            // TODO: Is there a better/cleaner way?
            _isServerSide = AppDomain.CurrentDomain.GetAssemblies().Any(p => p.GetName().Name == "Microsoft.AspNetCore.Mvc");
        }

        public async Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat,
            CancellationToken cancellationToken = new CancellationToken())
        {
            BlazorHttpConnection connection = new BlazorHttpConnection(_options, _jsRuntime, _uriHelper, _isServerSide, _loggerFactory);

            try
            {
                await connection.StartAsync(transferFormat);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        public Task DisposeAsync(ConnectionContext connection)
        {
            return ((BlazorHttpConnection)connection).DisposeAsync();
        }
    }
}