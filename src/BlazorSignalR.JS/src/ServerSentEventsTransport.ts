import { DotNetReferenceType } from './DotNet'

export class ServerSentEventsTransport {
    private connections: Map<string, EventSource> = new Map<string, EventSource>();

    public CreateConnection = (url: string, managedObj: DotNetReferenceType): void => {
        const id = managedObj.invokeMethod<string>("get_InternalSSEId");
        const token = managedObj.invokeMethod<string>("get_SSEAccessToken");

        if (token) {
            url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
        }

        const eventSource = new EventSource(url, { withCredentials: true });
        this.connections.set(id, eventSource);

        eventSource.onmessage = (e: MessageEvent) => {
            managedObj.invokeMethod<void>("HandleSSEMessage", btoa(e.data));
        };

        eventSource.onerror = (e: MessageEvent) => {
            const error = new Error(e.data || "Error occurred");
            managedObj.invokeMethod<void>("HandleSSEError", error.message);
        };

        eventSource.onopen = () => {
            managedObj.invokeMethod<void>("HandleSSEOpened");
        };
    }

    public CloseConnection = (managedObj: DotNetReferenceType): void => {
        const id = managedObj.invokeMethod<string>("get_InternalSSEId");

        const eventSource: EventSource = this.connections[id];
        this.connections.delete(id);

        eventSource.close();
    }

    public IsSupported = (): boolean => {
        return typeof EventSource !== "undefined";
    }
}