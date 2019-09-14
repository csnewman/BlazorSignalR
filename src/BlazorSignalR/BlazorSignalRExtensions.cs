using System;
using System.Net;
using BlazorSignalR.Internal;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;

namespace BlazorSignalR
{
    public static class BlazorSignalRExtensions
    {
        public static IHubConnectionBuilder WithUrlBlazor(this IHubConnectionBuilder hubConnectionBuilder, string url, IJSRuntime jsRuntime, NavigationManager navigationManager,
            HttpTransportType? transports = null, Action<BlazorHttpConnectionOptions> options = null)
        {
            return WithUrlBlazor(hubConnectionBuilder, new Uri(url), jsRuntime, navigationManager, transports, options);
        }

        public static IHubConnectionBuilder WithUrlBlazor(this IHubConnectionBuilder hubConnectionBuilder, Uri url, IJSRuntime jsRuntime, NavigationManager navigationManager,
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

            hubConnectionBuilder.Services.AddSingleton<EndPoint, BlazorHttpConnectionOptionsDerivedHttpEndPoint>();

            hubConnectionBuilder.Services.AddSingleton<IConfigureOptions<BlazorHttpConnectionOptions>, BlazorHubProtocolDerivedHttpOptionsConfigurer>();

            hubConnectionBuilder.Services.AddSingleton(provider => BuildBlazorHttpConnectionFactory(provider, jsRuntime, navigationManager));
            return hubConnectionBuilder;
        }

        private class BlazorHttpConnectionOptionsDerivedHttpEndPoint : UriEndPoint
        {
            public BlazorHttpConnectionOptionsDerivedHttpEndPoint(IOptions<BlazorHttpConnectionOptions> options)
                : base(options.Value.Url)
            {

            }
        }

        private class BlazorHubProtocolDerivedHttpOptionsConfigurer : IConfigureNamedOptions<BlazorHttpConnectionOptions>
        {
            private readonly TransferFormat _defaultTransferFormat;

            public BlazorHubProtocolDerivedHttpOptionsConfigurer(IHubProtocol hubProtocol)
            {
                 _defaultTransferFormat = hubProtocol.TransferFormat;
            }

            public void Configure(string name, BlazorHttpConnectionOptions options)
            {
                Configure(options);
            }

            public void Configure(BlazorHttpConnectionOptions options)
            {
                options.DefaultTransferFormat = _defaultTransferFormat;
            }
        }

        private static IConnectionFactory BuildBlazorHttpConnectionFactory(IServiceProvider provider, IJSRuntime jsRuntime, NavigationManager navigationManager)
        {
            return ActivatorUtilities.CreateInstance<BlazorHttpConnectionFactory>(
                provider,
                jsRuntime,
                navigationManager);
        }
    }
}