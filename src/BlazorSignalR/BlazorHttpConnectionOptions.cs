using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;

namespace BlazorSignalR
{
    public class BlazorHttpConnectionOptions
    {
        private IDictionary<string, string> _headers;

        public BlazorHttpConnectionOptions()
        {
            _headers = new Dictionary<string, string>();
            Transports = HttpTransports.All;
        }

        /// <summary>
        /// Gets or sets a delegate for wrapping or replacing the <see cref="P:Microsoft.AspNetCore.Http.Connections.Client.HttpConnectionOptions.HttpMessageHandlerFactory" />
        /// that will make HTTP requests.
        /// </summary>
        public Func<HttpMessageHandler, HttpMessageHandler> HttpMessageHandlerFactory { get; set; }

        /// <summary>
        /// Gets or sets a collection of headers that will be sent with HTTP requests.
        /// </summary>
        public IDictionary<string, string> Headers
        {
            get { return this._headers; }
            set
            {
                IDictionary<string, string> dictionary = value;
                _headers = dictionary ?? throw new ArgumentNullException(nameof(value));
            }
        }

        /// <summary>Gets or sets the URL used to send HTTP requests.</summary>
        public Uri Url { get; set; }

        /// <summary>
        /// Gets or sets a bitmask comprised of one or more <see cref="T:Microsoft.AspNetCore.Http.Connections.HttpTransportType" /> that specify what transports the client should use to send HTTP requests.
        /// </summary>
        public HttpTransportType Transports { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether negotiation is skipped when connecting to the server.
        /// </summary>
        /// <remarks>
        /// Negotiation can only be skipped when using the <see cref="F:Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets" /> transport.
        /// </remarks>
        public bool SkipNegotiation { get; set; }

        /// <summary>
        /// Gets or sets an access token provider that will be called to return a token for each HTTP request.
        /// </summary>
        public Func<Task<string>> AccessTokenProvider { get; set; }
    }
}