# BlazorSignalR
This package is a compatibility library for [Microsoft ASP.NET Core SignalR](https://github.com/aspnet/SignalR) to allow it to run on [Microsoft ASP.NET Blazor](https://github.com/aspnet/Blazor).

The package is an addon for the existing .net client for SingalR, this is unlike the ```BlazorExtensions/SignalR``` package which emulates the c# api. This package instead works by replacing the transport mechanics, meaning the front facing SignalR api is still the standard .net one.

For more information about SignalR development, please check [SignalR documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-2.1).

## How it works

You install the standard SingalR Core .NET Client and this package. And then via the following:

```
HubConnectionBuilder factory = new HubConnectionBuilder();

factory.WithUrlBlazor(new Uri("http://localhost:60071/chathub"), HttpTransportType.LongPolling,
    opt =>
    {
        opt.AccessTokenProvider = async () =>
        {
            return "some token for example";
        };
    });

factory.Services.AddLogging(builder => builder
    .AddBrowserConsole() // Add Blazor.Extensions.Logging.BrowserConsoleLogger
    .SetMinimumLevel(LogLevel.Trace)
);

HubConnection connection = factory.Build();
```

Currently LongPolling is the only working transport. ServerSentEvents and WebSockets are not supported in blazor, so therefore are not supported here yet.


## Alternatives

### [BlazorExtensions/SignalR](https://github.com/BlazorExtensions/SignalR)
Uses the typescript client, exposed to c# via a fake api that relays back to typescript. This has the benefit of being extreemly reliable and fast, however at the expense of each and very feature needing to be hand exposed, meaning the api does not perfectly reflect the .net api.

This package uses the test suite from the  package. Please do check it out!
