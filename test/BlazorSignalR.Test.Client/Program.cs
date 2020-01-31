using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using Microsoft.AspNetCore.Blazor.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorSignalR.Test.Client
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);
			builder.Services.AddLogging(); // We want to add (builder => builder.AddBrowserConsole()) here once it is available.
			builder.RootComponents.Add<App>("app");

			await builder.Build().RunAsync();
		}
	}
}
