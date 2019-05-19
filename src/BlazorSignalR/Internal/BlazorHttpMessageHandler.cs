// Based on:
// ASP.NET Core (https://github.com/aspnet/AspNetCore)
// https://github.com/aspnet/AspNetCore/blob/v3.0.0-preview5-19227-01/src/Components/Blazor/Blazor/src/Http/WebAssemblyHttpMessageHandler.cs
// https://github.com/aspnet/AspNetCore/blob/v3.0.0-preview5-19227-01/src/Components/Blazor/Blazor/src/Http/FetchCredentialsOption.cs
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. 

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace BlazorSignalR.Internal
{
    /// <summary>
    /// A browser-compatible implementation of <see cref="HttpMessageHandler"/>
    /// </summary>
    internal class BlazorHttpMessageHandler : HttpMessageHandler
    {
        /// <summary>
        /// Gets or sets the default value of the 'credentials' option on outbound HTTP requests.
        /// Defaults to <see cref="FetchCredentialsOption.SameOrigin"/>.
        /// </summary>
        public static FetchCredentialsOption DefaultCredentials { get; set; }
            = FetchCredentialsOption.SameOrigin;

        private readonly IJSRuntime _jsRuntime;

        /// <summary>
        /// The name of a well-known property that can be added to <see cref="HttpRequestMessage.Properties"/>
        /// to control the arguments passed to the underlying JavaScript <code>fetch</code> API.
        /// </summary>
        public const string FetchArgs = "BlazorHttpMessageHandler.FetchArgs";

        public BlazorHttpMessageHandler(IJSRuntime jsRuntime)
        {
            if (jsRuntime == null)
                throw new ArgumentNullException(nameof(jsRuntime));

            _jsRuntime = jsRuntime;
        }

        /// <inheritdoc />
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return WithCancellation(SendAsync(request), cancellationToken);
        }

        private async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            var options = new FetchOptions();
            if (request.Properties.TryGetValue(FetchArgs, out var fetchArgs))
            {
                options.RequestInitOverrides = fetchArgs;
            }

            options.RequestInit = new RequestInit
            {
                Credentials = GetDefaultCredentialsString(),
                Headers = GetHeadersAsStringArray(request),
                Method = request.Method.Method
            };

            options.RequestUri = request.RequestUri.ToString();

            var responseDescriptor = await _jsRuntime.InvokeAsync<ResponseDescriptor>(
                 "BlazorSignalR.BlazorHttpMessageHandler.sendAsync",
                 request.Content == null ? null : await request.Content.ReadAsByteArrayAsync(),
                 Json.Serialize(options));

            return responseDescriptor.ToResponseMessage();
        }

        private static Task<T> WithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            if (!cancellationToken.CanBeCanceled || task.IsCompleted)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<T>(cancellationToken);
            }

            return InternalWithCancellation(task, cancellationToken);
        }

        private static async Task<T> InternalWithCancellation<T>(Task<T> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<T>();
            var cancellationTask = tcs.Task;

            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), useSynchronizationContext: false))
            {
                var completed = await Task.WhenAny(tcs.Task, task).ConfigureAwait(false);

                if (completed == cancellationTask)
                {
                    Debug.Assert(cancellationToken.IsCancellationRequested);

                    HandleExceptions(task);
                }

                return await completed;
            }
        }

        private static void HandleExceptions(Task task)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));

            task.ContinueWith(t =>
            {
                if (t.Exception != null)
                {
                    Debug.WriteLine("An exception occured unexpectedly.");
                    Debug.WriteLine(t.Exception.InnerException.ToString());
                }
            });
        }

        private string[][] GetHeadersAsStringArray(HttpRequestMessage request)
        {
            return (from header in request.Headers.Concat(request.Content?.Headers ?? Enumerable.Empty<KeyValuePair<string, IEnumerable<string>>>())
                    from headerValue in header.Value // There can be more than one value for each name
                    select new[] { header.Key, headerValue }).ToArray();
        }

        private static string GetDefaultCredentialsString()
        {
            // See https://developer.mozilla.org/en-US/docs/Web/API/Request/credentials for
            // standard values and meanings
            switch (DefaultCredentials)
            {
                case FetchCredentialsOption.Omit:
                    return "omit";
                case FetchCredentialsOption.SameOrigin:
                    return "same-origin";
                case FetchCredentialsOption.Include:
                    return "include";
                default:
                    throw new ArgumentException($"Unknown credentials option '{DefaultCredentials}'.");
            }
        }

        // Keep these in sync with TypeScript class in BlazorHttpMessageHandler.ts
        private class FetchOptions
        {
            public string RequestUri { get; set; }
            public RequestInit RequestInit { get; set; }
            public object RequestInitOverrides { get; set; }
        }

        private class RequestInit
        {
            public string Credentials { get; set; }
            public string[][] Headers { get; set; }
            public string Method { get; set; }
        }

        private class ResponseDescriptor
        {
#pragma warning disable 0649
            public int StatusCode { get; set; }
            public string StatusText { get; set; }
            public string[][] Headers { get; set; }
            public byte[] BodyData { get; set; }
            public string ErrorText { get; set; }
#pragma warning restore 0649

            public HttpResponseMessage ToResponseMessage()
            {
                if (ErrorText != null)
                {
                    throw new HttpRequestException(ErrorText);
                }

                var responseContent = BodyData == null ? null : new ByteArrayContent(BodyData);
                var result = new HttpResponseMessage((HttpStatusCode)StatusCode)
                {
                    ReasonPhrase = StatusText,
                    Content = responseContent
                };
                var headers = result.Headers;
                var contentHeaders = result.Content?.Headers;
                foreach (var pair in Headers)
                {
                    if (!headers.TryAddWithoutValidation(pair[0], pair[1]))
                    {
                        contentHeaders?.TryAddWithoutValidation(pair[0], pair[1]);
                    }
                }

                return result;
            }
        }
    }

    /// <summary>
    /// Specifies a value for the 'credentials' option on outbound HTTP requests.
    /// </summary>
    internal enum FetchCredentialsOption
    {
        /// <summary>
        /// Advises the browser never to send credentials (such as cookies or HTTP auth headers).
        /// </summary>
        Omit,

        /// <summary>
        /// Advises the browser to send credentials (such as cookies or HTTP auth headers)
        /// only if the target URL is on the same origin as the calling application.
        /// </summary>
        SameOrigin,

        /// <summary>
        /// Advises the browser to send credentials (such as cookies or HTTP auth headers)
        /// even for cross-origin requests.
        /// </summary>
        Include,
    }
}
