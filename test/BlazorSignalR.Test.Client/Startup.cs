using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorSignalR.Test.Client
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(/*builder => builder.AddBrowserConsole()*/); // This is not yet available for Blazor 0.8.0 https://github.com/BlazorExtensions/Logging/pull/22
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
