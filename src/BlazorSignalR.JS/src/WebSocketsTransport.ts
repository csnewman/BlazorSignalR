import { DotNetReferenceType } from './DotNet'

export class WebSocketsTransport {
    private static connections: Map<string, WebSocket> = new Map<string, WebSocket>();

    public async CreateConnection (url: string, binary: boolean, managedObj: DotNetReferenceType): Promise<void> {
        const id = await managedObj.invokeMethodAsync<string>("get_InternalWebSocketId");
        const token = await managedObj.invokeMethodAsync<string>("get_WebSocketAccessToken");

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
            managedObj.invokeMethodAsync<void>("HandleWebSocketOpened");
        };

        webSocket.onerror = (event: Event) => {
            const error = (event instanceof ErrorEvent) ? event.error : new Error("Error occured");
            managedObj.invokeMethodAsync<void>("HandleWebSocketError", error.message);
        };

        webSocket.onmessage = (message: MessageEvent) => {
            managedObj.invokeMethodAsync<void>("HandleWebSocketMessage", btoa(message.data));
        };

        webSocket.onclose = (event: CloseEvent) => {
            managedObj.invokeMethodAsync<void>("HandleWebSocketClosed");
        };
    }

    public async Send (data: string, managedObj: DotNetReferenceType): Promise<void> {
        const id = await managedObj.invokeMethodAsync<string>("get_InternalWebSocketId");
        const webSocket = WebSocketsTransport.connections.get(id);

        if (!webSocket)
            throw new Error("Unknown connection");

        webSocket.send(atob(data));
    }

    public async CloseConnection(managedObj: DotNetReferenceType): Promise<void> {
        const id = await managedObj.invokeMethodAsync<string>("get_InternalWebSocketId");

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