using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BlazorSignalR.Internal
{
    internal class BlazorHttpConnectionFactory : IConnectionFactory
    {
        private readonly BlazorHttpConnectionOptions _options;
        private readonly ILoggerFactory _loggerFactory;

        public BlazorHttpConnectionFactory(IOptions<BlazorHttpConnectionOptions> options, ILoggerFactory loggerFactory)
        {
            _options = options.Value;
            _loggerFactory = loggerFactory;
        }

        public async Task<ConnectionContext> ConnectAsync(TransferFormat transferFormat,
            CancellationToken cancellationToken = new CancellationToken())
        {
            BlazorHttpConnection connection = new BlazorHttpConnection(_options, _loggerFactory);

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
            return ((BlazorHttpConnection) connection).DisposeAsync();
        }
    }
}