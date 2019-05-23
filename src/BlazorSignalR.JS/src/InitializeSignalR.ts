import { ServerSentEventsTransport } from './ServerSentEventsTransport'
import { WebSocketsTransport } from "./WebSocketsTransport";
import { BlazorHttpMessageHandler } from "./BlazorHttpMessageHandler";

namespace SignalR {
    const blazorSignalR: string = 'BlazorSignalR';
    // define what this extension adds to the window object inside BlazorSignalR
    const extensionObject = {
        ServerSentEventsTransport: new ServerSentEventsTransport(),
        WebSocketsTransport: new WebSocketsTransport(),
        BlazorHttpMessageHandler: new BlazorHttpMessageHandler()
    };

    export function initialize(): void {
        if (typeof window !== 'undefined' && !window[blazorSignalR]) {
            // when the library is loaded in a browser via a <script> element, make the
            // following APIs available in global scope for invocation from JS
            window[blazorSignalR] = {
                ...extensionObject
            };
        } else {
            window[blazorSignalR] = {
                ...window[blazorSignalR],
                ...extensionObject
            };
        }
    }
}

SignalR.initialize();