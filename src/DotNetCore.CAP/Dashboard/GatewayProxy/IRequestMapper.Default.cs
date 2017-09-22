using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace DotNetCore.CAP.Dashboard.GatewayProxy
{
    public class RequestMapper : IRequestMapper
    {
        private readonly string[] _unsupportedHeaders = { "host" };
        private const string SchemeDelimiter = "://";

        public async Task<HttpRequestMessage> Map(HttpRequest request)
        {
            try
            {
                var requestMessage = new HttpRequestMessage()
                {
                    Content = await MapContent(request),
                    Method = MapMethod(request),
                    RequestUri = MapUri(request)
                };

                MapHeaders(request, requestMessage);

                return requestMessage;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error when parsing incoming request, exception: {ex.Message}");
            }
        }

        private async Task<HttpContent> MapContent(HttpRequest request)
        {
            if (request.Body == null)
            {
                return null;
            }

            var content = new ByteArrayContent(await ToByteArray(request.Body));

            content.Headers.TryAddWithoutValidation("Content-Type", new[] { request.ContentType });

            return content;
        }

        private HttpMethod MapMethod(HttpRequest request)
        {
            return new HttpMethod(request.Method);
        }

        private Uri MapUri(HttpRequest request)
        {
            return new Uri(GetEncodedUrl(request));
        }

        private void MapHeaders(HttpRequest request, HttpRequestMessage requestMessage)
        {
            foreach (var header in request.Headers)
            {
                //todo get rid of if..
                if (IsSupportedHeader(header))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        private async Task<byte[]> ToByteArray(Stream stream)
        {
            using (stream)
            {
                using (var memStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memStream);
                    return memStream.ToArray();
                }
            }
        }

        private bool IsSupportedHeader(KeyValuePair<string, StringValues> header)
        {
            return !_unsupportedHeaders.Contains(header.Key.ToLower());
        }

        /// <summary>
        /// Combines the given URI components into a string that is properly encoded for use in HTTP headers.
        /// Note that unicode in the HostString will be encoded as punycode.
        /// </summary>
        /// <param name="scheme">http, https, etc.</param>
        /// <param name="host">The host portion of the uri normally included in the Host header. This may include the port.</param>
        /// <param name="pathBase">The first portion of the request path associated with application root.</param>
        /// <param name="path">The portion of the request path that identifies the requested resource.</param>
        /// <param name="query">The query, if any.</param>
        /// <param name="fragment">The fragment, if any.</param>
        /// <returns></returns>
        public string BuildAbsolute(
            string scheme,
            HostString host,
            PathString pathBase = new PathString(),
            PathString path = new PathString(),
            QueryString query = new QueryString(),
            FragmentString fragment = new FragmentString())
        {
            if (scheme == null)
            {
                throw new ArgumentNullException(nameof(scheme));
            }

            var combinedPath = (pathBase.HasValue || path.HasValue) ? (pathBase + path).ToString() : "/";

            var encodedHost = host.ToString();
            var encodedQuery = query.ToString();
            var encodedFragment = fragment.ToString();

            // PERF: Calculate string length to allocate correct buffer size for StringBuilder.
            var length = scheme.Length + SchemeDelimiter.Length + encodedHost.Length
                + combinedPath.Length + encodedQuery.Length + encodedFragment.Length;

            return new StringBuilder(length)
                .Append(scheme)
                .Append(SchemeDelimiter)
                .Append(encodedHost)
                .Append(combinedPath)
                .Append(encodedQuery)
                .Append(encodedFragment)
                .ToString();
        }

        /// <summary>
        /// Returns the combined components of the request URL in a fully escaped form suitable for use in HTTP headers
        /// and other HTTP operations.
        /// </summary>
        /// <param name="request">The request to assemble the uri pieces from.</param>
        /// <returns></returns>
        public string GetEncodedUrl(HttpRequest request)
        {
            return BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path, request.QueryString);
        }
    }
}