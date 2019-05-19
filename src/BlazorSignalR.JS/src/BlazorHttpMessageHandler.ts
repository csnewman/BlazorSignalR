export class BlazorHttpMessageHandler {
    async sendAsync(body: number[], jsonFetchArgs: string): Promise<ResponseDescriptor> {
        let response: Response;
        let responseData: ArrayBuffer;

        const fetchOptions: FetchOptions = JSON.parse(jsonFetchArgs);
        const requestInit: RequestInit = Object.assign(fetchOptions.requestInit, fetchOptions.requestInitOverrides);

        if (body) {
            requestInit.body = new Uint8Array(body);
        }

        try {
            response = await fetch(fetchOptions.requestUri, requestInit);
            responseData = await response.arrayBuffer();
        } catch (ex) {
            return getErrorResponse(ex.toString());
        }

        return getSuccessResponse(response, responseData);
    }
}

function getSuccessResponse(response: Response, responseData: ArrayBuffer): ResponseDescriptor {
    const typedResponseData = new Uint8Array(responseData);

    const responseDescriptor: ResponseDescriptor = {
        statusCode: response.status,
        statusText: response.statusText,
        headers: [],
        bodyData: Array.from(typedResponseData),
        errorText: null
    };

    response.headers.forEach((value, name) => {
        responseDescriptor.headers.push([name, value]);
    });

    return responseDescriptor;
}

function getErrorResponse(errorMessage: string): ResponseDescriptor {
    const responseDescriptor: ResponseDescriptor = {
        statusCode: null,
        statusText: null,
        headers: [],
        bodyData: null,
        errorText: errorMessage
    };

    return responseDescriptor;
}

// Keep these in sync with the .NET equivalent in BlazorHttpMessageHandler.cs
interface FetchOptions {
    requestUri: string;
    requestInit: RequestInit;
    requestInitOverrides: RequestInit;
}

interface ResponseDescriptor {
    statusCode: number | null;
    statusText: string | null;
    headers: string[][];
    bodyData: number[] | null;
    errorText: string | null;
}
