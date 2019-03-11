using System;
using BlazorSignalR.Internal;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace BlazorSignalR
{
    public static class BlazorSignalRExtensions
    {
        public static IHubConnectionBuilder WithUrlBlazor(this IHubConnectionBuilder hubConnectionBuilder, string url, IJSRuntime jsRuntime,
            HttpTransportType? transports = null, Action<BlazorHttpConnectionOptions> options = null)
        {
            return WithUrlBlazor(hubConnectionBuilder, new Uri(url), jsRuntime, transports, options);
        }

        public static IHubConnectionBuilder WithUrlBlazor(this IHubConnectionBuilder hubConnectionBuilder, Uri url, IJSRuntime jsRuntime,
            HttpTransportType? transports = null, Action<BlazorHttpConnectionOptions> options = null)
        {
            if (hubConnectionBuilder == null)
                throw new ArgumentNullException(nameof(hubConnectionBuilder));

            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            hubConnectionBuilder.Services.Configure<BlazorHttpConnectionOptions>(o =>
            {
                o.Url = url;
                if (!transports.HasValue)
                    return;
                o.Transports = transports.Value;
            });
            if (options != null)
                hubConnectionBuilder.Services.Configure(options);
            hubConnectionBuilder.Services.AddSingleton(provider => BuildBlazorHttpConnectionFactory(provider, jsRuntime));
            return hubConnectionBuilder;
        }

        private static IConnectionFactory BuildBlazorHttpConnectionFactory(IServiceProvider provider, IJSRuntime jsRuntime)
        {
            return ActivatorUtilities.CreateInstance<BlazorHttpConnectionFactory>(
                provider,
                jsRuntime as IJSInProcessRuntime ?? new JSRuntimeWrapper(jsRuntime));
        }
    }
}