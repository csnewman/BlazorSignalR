# BlazorSignalR
This package is a compatibility library for [Microsoft ASP.NET Core SignalR](https://github.com/aspnet/SignalR) to allow it to run on [Microsoft ASP.NET Blazor](https://github.com/aspnet/Blazor).

The package is an addon for the existing .net client for SingalR, this is unlike the ```BlazorExtensions/SignalR``` package which emulates the c# api. This package instead works by replacing the transport mechanics, meaning the front facing SignalR api is still the standard .net one. The benefits of this setup is that as SignalR changes, this package takes little maintenance, as it only replaces the transport mechanisms, which are unlikely to change.

For more information about SignalR development, please check [SignalR documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/introduction?view=aspnetcore-2.1).

## Features

- Uses standard SignalR Core .NET client
- Supports Long Polling & Server Side Events transports
- Allows multiple hub connections
- Wide compatability (Automatic transport fallback ensure it works on all platforms)

## How it works

You will need to install the standard SingalR Core .NET Client and this package. And then configure your connection creation like the following:

```
HubConnectionBuilder factory = new HubConnectionBuilder();

factory.WithUrlBlazor(new Uri("http://localhost:60071/chathub"), null,
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

## Transports
You can manually select what transports (and the implementations to use) via ```Transports``` & ```Implementations``` in the ```BlazorHttpConnectionOptions``` when adding to the hub connection factory.

Working:

- Long Polling (Implemented in C#)
- Server Side Events (Implemented in JS, C# implementation waiting on Blazor bug)

Not Working:

- Web Sockets (JS version coming soon, C# implementation waiting on mono support)

## Alternatives

### [BlazorExtensions/SignalR](https://github.com/BlazorExtensions/SignalR)
Uses the typescript client, exposed to c# via a fake api that relays back to typescript. This has the benefit of being extreemly reliable and fast, however at the expense of each and every feature needing to be hand exposed, meaning the api does not perfectly reflect the .net api.

This package uses the test suite from the ```BlazorExtensions/SignalR``` package and was based on their work. Please do check it out!
