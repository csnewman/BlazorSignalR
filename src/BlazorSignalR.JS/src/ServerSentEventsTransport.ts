import { DotNetReferenceType } from './DotNet'

export class ServerSentEventsTransport {
    private connections: Map<string, EventSource> = new Map<string, EventSource>();

    public async CreateConnection(url: string, managedObj: DotNetReferenceType): Promise<void> {
        const id = await managedObj.invokeMethodAsync<string>("get_InternalSSEId");
        const token = await managedObj.invokeMethodAsync<string>("get_SSEAccessToken");

        if (token) {
            url += (url.indexOf("?") < 0 ? "?" : "&") + `access_token=${encodeURIComponent(token)}`;
        }

        const eventSource = new EventSource(url, { withCredentials: true });
        this.connections.set(id, eventSource);

        eventSource.onmessage = async (e: MessageEvent) => {
            await managedObj.invokeMethodAsync<void>("HandleSSEMessage", btoa(e.data));
        };

        eventSource.onerror = async (e: MessageEvent) => {
            const error = new Error(e.data || "Error occurred");
            await managedObj.invokeMethodAsync<void>("HandleSSEError", error.message);
        };

        eventSource.onopen = async () => {
            await managedObj.invokeMethodAsync<void>("HandleSSEOpened");
        };
    }

    public async CloseConnection(managedObj: DotNetReferenceType): Promise<void> {
        const id = await managedObj.invokeMethodAsync<string>("get_InternalSSEId");

        const eventSource = this.connections.get(id);

        if (!eventSource)
            return;

        this.connections.delete(id);

        eventSource.close();
    }

    public IsSupported = (): boolean => {
        return typeof EventSource !== "undefined";
    }
}