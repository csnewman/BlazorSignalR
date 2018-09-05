import { DotNetReferenceType } from './DotNet'

export class WebSocketsTransport {
    private static connections: Map<string, WebSocket> = new Map<string, WebSocket>();

    public CreateConnection = (url: string, binary: boolean, managedObj: DotNetReferenceType): void => {
        const id = managedObj.invokeMethod<string>("get_InternalWebSocketId");
        const token = managedObj.invokeMethod<string>("get_WebSocketAccessToken");

        if (token) {
            url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
        }

        url = url.replace(/^http/, "ws");

        const webSocket = new WebSocket(url);
        WebSocketsTransport.connections.set(id, webSocket);
        
        if (binary) {
            webSocket.binaryType = "arraybuffer";
        }

        webSocket.onopen = (_event: Event) => {
            managedObj.invokeMethod<void>("HandleWebSocketOpened");
        };

        webSocket.onerror = (event: Event) => {
            const error = (event instanceof ErrorEvent) ? event.error : new Error("Error occured");
            managedObj.invokeMethod<void>("HandleWebSocketError", error.message);
        };

        webSocket.onmessage = (message: MessageEvent) => {
            managedObj.invokeMethod<void>("HandleWebSocketMessage", btoa(message.data));
        };

        webSocket.onclose = (event: CloseEvent) => {
            managedObj.invokeMethod<void>("HandleWebSocketClosed");
        };
    }

    public Send = (data: string, managedObj: DotNetReferenceType): void => {
        const id = managedObj.invokeMethod<string>("get_InternalWebSocketId");
        const webSocket = WebSocketsTransport.connections.get(id);

        if (!webSocket)
            throw new Error("Unknown connection");

        webSocket.send(atob(data));
    }

    public CloseConnection = (managedObj: DotNetReferenceType): void => {
        const id = managedObj.invokeMethod<string>("get_InternalWebSocketId");

        const webSocket = WebSocketsTransport.connections.get(id);

        if (!webSocket)
            return;

        WebSocketsTransport.connections.delete(id);

        webSocket.onclose = () => {};
        webSocket.onmessage = () => {};
        webSocket.onerror = () => {};
        webSocket.close();
    }

    public IsSupported = (): boolean => {
        return typeof WebSocket !== "undefined";
    }
}