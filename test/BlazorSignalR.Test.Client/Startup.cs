using Blazor.Extensions.Logging;
using BlazorSignalR.Test.Client.Services;
using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorSignalR.Test.Client
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(builder => builder.AddBrowserConsole());
            services.AddSingleton<IJwtTokenResolver, ClientSideJwtTokenResolver>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
