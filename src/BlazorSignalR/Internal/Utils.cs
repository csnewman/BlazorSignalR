using System;

namespace BlazorSignalR.Internal
{
    internal static class Utils
    {
        public static Uri AppendPath(Uri url, string path)
        {
            UriBuilder uriBuilder = new UriBuilder(url);
            if (!uriBuilder.Path.EndsWith("/"))
                uriBuilder.Path += "/";
            uriBuilder.Path += path;
            return uriBuilder.Uri;
        }

        internal static Uri AppendQueryString(Uri url, string qs)
        {
            if (string.IsNullOrEmpty(qs))
                return url;
            UriBuilder uriBuilder = new UriBuilder(url);
            string query = uriBuilder.Query;
            if (!string.IsNullOrEmpty(uriBuilder.Query))
                query += "&";
            string str = query + qs;
            if (str.Length > 0 && str[0] == '?')
                str = str.Substring(1);
            uriBuilder.Query = str;
            return uriBuilder.Uri;
        }
    }
}