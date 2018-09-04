using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace BlazorSignalR.Internal
{
    internal class BlazorAccessTokenHttpMessageHandler : DelegatingHandler
    {
        private readonly BlazorHttpConnection _httpConnection;

        public BlazorAccessTokenHttpMessageHandler(HttpMessageHandler inner, BlazorHttpConnection httpConnection)
            : base(inner)
        {
            this._httpConnection = httpConnection;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string accessTokenAsync = await _httpConnection.GetAccessTokenAsync();
            if (!string.IsNullOrEmpty(accessTokenAsync))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessTokenAsync);
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
