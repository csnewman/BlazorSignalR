using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Blazor.Extensions.Logging;
using BlazorSignalR.Test.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorSignalR.Test.Client.Pages
{
    public class ChatComponent : ComponentBase
    {
        [Inject] private IJwtTokenResolver JwtTokenResolver { get; set; }
        [Inject] private ILogger<ChatComponent> Logger { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private IUriHelper UriHelper { get; set; }

        // Needed to detect prerendering
        [Inject] private IComponentContext ComponentContext { get; set; }

        internal string ToEverybody { get; set; }
        internal string ToConnection { get; set; }
        internal string ConnectionId { get; set; }
        internal string ToMe { get; set; }
        internal string ToGroup { get; set; }
        internal string GroupName { get; set; }
        internal List<string> Messages { get; set; } = new List<string>();

        private IDisposable _objectHandle;
        private IDisposable _listHandle;
        private HubConnection _connection;

        private bool _connectionInitalized = false;

        protected override async Task OnAfterRenderAsync()
        {
            if (!ComponentContext.IsConnected)
            {
                return;
            }

            if (!_connectionInitalized)
            {
                await InitConnectionAsync();
                _connectionInitalized = true;
            }
        }

        private async Task InitConnectionAsync()
        {
            var factory = new HubConnectionBuilder();

            // Put the js-runtime in there in order that the browser logger can consume it.
            factory.Services.AddSingleton(JsRuntime);

            factory.Services.AddLogging(builder => builder
                .AddBrowserConsole()
                .SetMinimumLevel(LogLevel.Trace)
            );

            factory.WithUrlBlazor("/chathub", JsRuntime, UriHelper, options: opt =>
            {
                // opt.Transports = HttpTransportType.WebSockets;
                // opt.SkipNegotiation = true;
                opt.AccessTokenProvider = async () =>
                {
                    var token = await JwtTokenResolver.GetJwtTokenAsync("DemoUser");
                    Logger.LogInformation($"Access Token: {token}");
                    return token;
                };
            });

            _connection = factory.Build();

            _connection.On<string>("Send", HandleTest);

            _connection.Closed += exception =>
            {
                Logger.LogError(exception, "Connection was closed!");
                return Task.CompletedTask;
            };
            await _connection.StartAsync();
        }

        private void HandleTest(string obj)
        {
            Handle(obj);
        }

        public void DemoMethodObject(DemoData data)
        {
            Logger.LogInformation("Got object!");
            Logger.LogInformation(data?.GetType().FullName ?? "<NULL>");
            _objectHandle.Dispose();
            if (data == null)
                return;
            Handle(data);
        }

        public void DemoMethodList(DemoData[] data)
        {
            Logger.LogInformation("Got List!");
            Logger.LogInformation(data?.GetType().FullName ?? "<NULL>");
            _listHandle.Dispose();
            if (data == null)
                return;
            Handle(data);
        }

        private void Handle(object msg)
        {
            Logger.LogInformation(msg.ToString());
            Messages.Add(msg.ToString());
            StateHasChanged();
        }

        internal async Task Broadcast()
        {
            await _connection.InvokeAsync("Send", ToEverybody);
        }

        internal async Task SendToOthers()
        {
            await _connection.InvokeAsync("SendToOthers", ToEverybody);
        }

        internal async Task SendToConnection()
        {
            await _connection.InvokeAsync("SendToConnection", ConnectionId, ToConnection);
        }

        internal async Task SendToMe()
        {
            await _connection.InvokeAsync("Echo", ToMe);
        }

        internal async Task SendToGroup()
        {
            await _connection.InvokeAsync("SendToGroup", GroupName, ToGroup);
        }

        internal async Task SendToOthersInGroup()
        {
            await _connection.InvokeAsync("SendToOthersInGroup", GroupName, ToGroup);
        }

        internal async Task JoinGroup()
        {
            await _connection.InvokeAsync("JoinGroup", GroupName);
        }

        internal async Task LeaveGroup()
        {
            await _connection.InvokeAsync("LeaveGroup", GroupName);
        }

        internal async Task TellHubToDoStuff()
        {
            _objectHandle = _connection.On<DemoData>("DemoMethodObject", DemoMethodObject);
            _listHandle = _connection.On<DemoData[]>("DemoMethodList", DemoMethodList);
            await _connection.InvokeAsync("DoSomething");
        }
    }
}
