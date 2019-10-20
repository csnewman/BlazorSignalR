using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
//using Blazor.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorSignalR.Test.Client.Pages
{
    public class ChatComponent : ComponentBase
    {
        [Inject] private HttpClient Http { get; set; }
        [Inject] private ILogger<ChatComponent> Logger { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private NavigationManager NavigationManager { get; set; }
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

        protected override async Task OnInitializedAsync()
        {
            HubConnectionBuilder factory = new HubConnectionBuilder();

            factory.Services.AddLogging(builder => builder
                //.AddBrowserConsole() // Add Blazor.Extensions.Logging.BrowserConsoleLogger // This is not yet available for Blazor 0.8.0 https://github.com/BlazorExtensions/Logging/pull/22
                .SetMinimumLevel(LogLevel.Trace)
            );

            factory.WithUrlBlazor("/chathub", JsRuntime, NavigationManager, options: opt =>
            {
                //opt.Transports = HttpTransportType.WebSockets;
                //opt.SkipNegotiation = true;
                opt.AccessTokenProvider = async () =>
                {
                    var token = await this.GetJwtToken("DemoUser");
                    this.Logger.LogInformation($"Access Token: {token}");
                    return token;
                };
            });

            this._connection = factory.Build();

            this._connection.On<string>("Send", this.HandleTest);

            _connection.Closed += exception =>
            {
                this.Logger.LogError(exception, "Connection was closed!");
                return Task.CompletedTask;
            };
            await this._connection.StartAsync();
        }

        private void HandleTest(string obj)
        {
            Handle(obj);
        }

        public void DemoMethodObject(DemoData data)
        {
            this.Logger.LogInformation("Got object!");
            this.Logger.LogInformation(data?.GetType().FullName ?? "<NULL>");
            this._objectHandle.Dispose();
            if (data == null) return;
            this.Handle(data);
        }

        public void DemoMethodList(DemoData[] data)
        {
            this.Logger.LogInformation("Got List!");
            this.Logger.LogInformation(data?.GetType().FullName ?? "<NULL>");
            this._listHandle.Dispose();
            if (data == null) return;
            this.Handle(data);
        }

        private async Task<string> GetJwtToken(string userId)
        {
            var httpResponse = await this.Http.GetAsync($"{ GetBaseAddress()}generatetoken?user={userId}");
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        private string GetBaseAddress()
        {
            var baseAddress = Http.BaseAddress.ToString();

            if (!baseAddress.EndsWith("/"))
            {
                baseAddress += "/";
            }

            return baseAddress;
        }

        private void Handle(object msg)
        {
            if(msg is DemoData)
            {
                var demoData = msg as DemoData;
                this.Messages.Add($"demoData.id({demoData.Id}) | demoData.Data({demoData.Data}) | demoData.DateTime({demoData.DateTime}) | demoData.Decimal({demoData.Decimal}) | demoData.Bool({demoData.Bool})");
            }
            else if(msg is DemoData[])
            {
                var demoDatas = msg as DemoData[];
                foreach (var demoData in demoDatas)
                {
                    this.Messages.Add($"demoData.id({demoData.Id}) | demoData.Data({demoData.Data}) | demoData.DateTime({demoData.DateTime}) | demoData.Decimal({demoData.Decimal}) | demoData.Bool({demoData.Bool})");
                }
            }
            else
            {
                this.Logger.LogInformation(msg.ToString());
            }
            this.Messages.Add(msg.ToString());
            this.StateHasChanged();
        }

        internal async Task Broadcast()
        {
            await this._connection.InvokeAsync("Send", this.ToEverybody);
        }

        internal async Task SendToOthers()
        {
            await this._connection.InvokeAsync("SendToOthers", this.ToEverybody);
        }

        internal async Task SendToConnection()
        {
            await this._connection.InvokeAsync("SendToConnection", this.ConnectionId, this.ToConnection);
        }

        internal async Task SendToMe()
        {
            await this._connection.InvokeAsync("Echo", this.ToMe);
        }

        internal async Task SendToGroup()
        {
            await this._connection.InvokeAsync("SendToGroup", this.GroupName, this.ToGroup);
        }

        internal async Task SendToOthersInGroup()
        {
            await this._connection.InvokeAsync("SendToOthersInGroup", this.GroupName, this.ToGroup);
        }

        internal async Task JoinGroup()
        {
            await this._connection.InvokeAsync("JoinGroup", this.GroupName);
        }

        internal async Task LeaveGroup()
        {
            await this._connection.InvokeAsync("LeaveGroup", this.GroupName);
        }

        internal async Task TellHubToDoStuff()
        {
            this._objectHandle = this._connection.On<DemoData>("DemoMethodObject", this.DemoMethodObject);
            this._listHandle = this._connection.On<DemoData[]>("DemoMethodList", this.DemoMethodList);
            await this._connection.InvokeAsync("DoSomething");
        }
    }
}