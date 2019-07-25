using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;

namespace BlazorSignalR.Internal
{
    public static class DuplexPipeExtensions
    {
        public static Task StartAsync(this IDuplexPipe pipe, Uri uri, TransferFormat transferFormat)
        {
            var method = pipe.GetType().GetMethod(nameof(StartAsync), new Type[] { typeof(Uri), typeof(TransferFormat), typeof(CancellationToken) });
            return (Task)method.Invoke(pipe, new object[] { uri, transferFormat, default(CancellationToken) });
        }

        public static Task StopAsync(this IDuplexPipe pipe)
        {
            var method = pipe.GetType().GetMethod(nameof(StopAsync), new Type[] { });
            return (Task)method.Invoke(pipe, new object[] { });
        }
    }
}