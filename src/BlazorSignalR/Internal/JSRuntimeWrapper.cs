using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorSignalR.Internal
{
    internal sealed class JSRuntimeWrapper : IJSInProcessRuntime
    {
        private readonly IJSRuntime _jsRuntime;

        public JSRuntimeWrapper(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public T Invoke<T>(string identifier, params object[] args)
        {
            var task = _jsRuntime.InvokeAsync<T>(identifier, args);
            return task.ConfigureAwait(false).GetAwaiter().GetResult();
        }

        public Task<T> InvokeAsync<T>(string identifier, params object[] args)
        {
            return _jsRuntime.InvokeAsync<T>(identifier, args);
        }

        public void UntrackObjectRef(DotNetObjectRef dotNetObjectRef)
        {
            _jsRuntime.UntrackObjectRef(dotNetObjectRef);
        }
    }
}