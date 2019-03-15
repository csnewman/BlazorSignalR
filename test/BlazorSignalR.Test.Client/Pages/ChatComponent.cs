using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
//using Blazor.Extensions.Logging;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace BlazorSignalR.Test.Client.Pages
{
    public class ChatComponent : ComponentBase
    {
        [Inject] private HttpClient _http { get; set; }
        [Inject] private ILogger<ChatComponent> _logger { get; set; }
        [Inject] private IJSRuntime _jsRuntime { get; set; }
        [Inject] private IUriHelper _uriHelper { get; set; }
        internal string _toEverybody { get; set; }
        internal string _toConnection { get; set; }
        internal string _connectionId { get; set; }
        internal string _toMe { get; set; }
        internal string _toGroup { get; set; }
        internal string _groupName { get; set; }
        internal List<string> _messages { get; set; } = new List<string>();

        private IDisposable _objectHandle;
        private IDisposable _listHandle;
        private HubConnection _connection;

        protected override async Task OnInitAsync()
        {
            // https://github.com/aspnet/AspNetCore/issues/8327
            // https://github.com/aspnet/AspNetCore/issues/8404
            // if(Prednering)
            // { return; }

            HubConnectionBuilder factory = new HubConnectionBuilder();

            factory.Services.AddLogging(builder => builder
                //.AddBrowserConsole() // Add Blazor.Extensions.Logging.BrowserConsoleLogger // This is not yet available for Blazor 0.8.0 https://github.com/BlazorExtensions/Logging/pull/22
                .SetMinimumLevel(LogLevel.Trace)
            );

            factory.WithUrlBlazor("/chathub", _jsRuntime, _uriHelper, options: opt =>
            {
                    //                opt.Transports = HttpTransportType.WebSockets;
                    //                opt.SkipNegotiation = true;
                    opt.AccessTokenProvider = async () =>
                        {
                    var token = await this.GetJwtToken("DemoUser");
                    this._logger.LogInformation($"Access Token: {token}");
                    return token;
                };
            });

            this._connection = factory.Build();

            this._connection.On<string>("Send", this.HandleTest);

            _connection.Closed += exception =>
            {
                this._logger.LogError(exception, "Connection was closed!");
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
            this._logger.LogInformation("Got object!");
            this._logger.LogInformation(data?.GetType().FullName ?? "<NULL>");
            this._objectHandle.Dispose();
            if (data == null) return;
            this.Handle(data);
        }

        public void DemoMethodList(DemoData[] data)
        {
            this._logger.LogInformation("Got List!");
            this._logger.LogInformation(data?.GetType().FullName ?? "<NULL>");
            this._listHandle.Dispose();
            if (data == null) return;
            this.Handle(data);
        }

        private async Task<string> GetJwtToken(string userId)
        {
            var httpResponse = await this._http.GetAsync($"/generatetoken?user={userId}");
            httpResponse.EnsureSuccessStatusCode();
            return await httpResponse.Content.ReadAsStringAsync();
        }

        private void Handle(object msg)
        {
            this._logger.LogInformation(msg.ToString());
            this._messages.Add(msg.ToString());
            this.StateHasChanged();
        }

        internal async Task Broadcast()
        {
            await this._connection.InvokeAsync("Send", this._toEverybody);
        }

        internal async Task SendToOthers()
        {
            await this._connection.InvokeAsync("SendToOthers", this._toEverybody);
        }

        internal async Task SendToConnection()
        {
            await this._connection.InvokeAsync("SendToConnection", this._connectionId, this._toConnection);
        }

        internal async Task SendToMe()
        {
            await this._connection.InvokeAsync("Echo", this._toMe);
        }

        internal async Task SendToGroup()
        {
            await this._connection.InvokeAsync("SendToGroup", this._groupName, this._toGroup);
        }

        internal async Task SendToOthersInGroup()
        {
            await this._connection.InvokeAsync("SendToOthersInGroup", this._groupName, this._toGroup);
        }

        internal async Task JoinGroup()
        {
            await this._connection.InvokeAsync("JoinGroup", this._groupName);
        }

        internal async Task LeaveGroup()
        {
            await this._connection.InvokeAsync("LeaveGroup", this._groupName);
        }

        internal async Task TellHubToDoStuff()
        {
            this._objectHandle = this._connection.On<DemoData>("DemoMethodObject", this.DemoMethodObject);
            this._listHandle = this._connection.On<DemoData[]>("DemoMethodList", this.DemoMethodList);
            await this._connection.InvokeAsync("DoSomething");
        }
    }
}