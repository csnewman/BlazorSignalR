using System;
using BlazorSignalR.Internal;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorSignalR
{
    public static class BlazorSignalRExtensions
    {
        public static IHubConnectionBuilder WithUrlBlazor(this IHubConnectionBuilder hubConnectionBuilder, string url,
            HttpTransportType? transports = null, Action<BlazorHttpConnectionOptions> options = null)
        {
            return WithUrlBlazor(hubConnectionBuilder, new Uri(url), transports, options);
        }

        public static IHubConnectionBuilder WithUrlBlazor(this IHubConnectionBuilder hubConnectionBuilder, Uri url,
            HttpTransportType? transports = null, Action<BlazorHttpConnectionOptions> options = null)
        {
            if (hubConnectionBuilder == null)
                throw new ArgumentNullException(nameof(hubConnectionBuilder));
            hubConnectionBuilder.Services.Configure<BlazorHttpConnectionOptions>(o =>
            {
                o.Url = url;
                if (!transports.HasValue)
                    return;
                o.Transports = transports.Value;
            });
            if (options != null)
                hubConnectionBuilder.Services.Configure(options);
            hubConnectionBuilder.Services.AddSingleton<IConnectionFactory, BlazorHttpConnectionFactory>();
            return hubConnectionBuilder;
        }
    }
}