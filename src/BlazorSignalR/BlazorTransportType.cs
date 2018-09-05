using System;
using System.Collections.Generic;
using System.Text;

namespace BlazorSignalR
{
    [Flags]
    public enum BlazorTransportType
    {
        /// <summary>Specifies that no transport is used.</summary>
        None = 0,

        /// <summary>Specifies that the web sockets transport is used. (C# implementation)</summary>
        ManagedWebSockets = 1,

        /// <summary>Specifies that the web sockets transport is used. (JS implementation)</summary>
        JsWebSockets = 2,

        /// <summary>
        /// Specifies that the server sent events transport is used. (C# implementation)
        /// </summary>
        ManagedServerSentEvents = 4,

        /// <summary>
        /// Specifies that the server sent events transport is used. (JS implementation)
        /// </summary>
        JsServerSentEvents = 8,

        /// <summary>Specifies that the long polling transport is used. (C# implementation)</summary>
        ManagedLongPolling = 16,

        /// <summary>Specifies that the long polling transport is used. (JS implementation)</summary>
        JsLongPolling = 32
    }
}