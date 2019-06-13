using System;
using System.Threading;
using System.Threading.Tasks;
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

        public BlazorHttpConnectionFactory(IOptions<BlazorHttpConnectionOptions> options, IJSRuntime jsRuntime, ILoggerFactory loggerFactory)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            _options = options.Value;
            _jsRuntime = jsRuntime;
            _loggerFactory = loggerFactory;
        }

        public async Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat,
            CancellationToken cancellationToken = new CancellationToken())
        {
            BlazorHttpConnection connection = new BlazorHttpConnection(_options, _jsRuntime, _loggerFactory);

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

        public async Task DisposeAsync(ConnectionContext connection)
        {
            await ((BlazorHttpConnection)connection).DisposeAsync();
        }
    }
}