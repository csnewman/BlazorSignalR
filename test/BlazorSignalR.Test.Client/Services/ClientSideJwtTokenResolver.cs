using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlazorSignalR.Test.Client.Services
{
    public sealed class ClientSideJwtTokenResolver : IJwtTokenResolver
    {
        private readonly HttpClient _httpClient;

        public ClientSideJwtTokenResolver(HttpClient httpClient)
        {
            if (httpClient == null)
                throw new ArgumentNullException(nameof(httpClient));

            _httpClient = httpClient;
        }

        public async Task<string> GetJwtTokenAsync(string userId)
        {
            var httpResponse = await _httpClient.GetAsync($"{ GetBaseAddress()}generatetoken?user={userId}");
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        private string GetBaseAddress()
        {
            var baseAddress = _httpClient.BaseAddress.ToString();

            if (!baseAddress.EndsWith("/"))
            {
                baseAddress += "/";
            }

            return baseAddress;
        }
    }
}
