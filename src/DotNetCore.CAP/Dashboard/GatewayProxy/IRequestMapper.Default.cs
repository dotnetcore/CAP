// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

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
        private const string SchemeDelimiter = "://";
        private readonly string[] _unsupportedHeaders = {"host", "cookie"};

        public async Task<HttpRequestMessage> Map(HttpRequest request)
        {
            try
            {
                var requestMessage = new HttpRequestMessage
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

        private string BuildAbsolute(
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

            var combinedPath = pathBase.HasValue || path.HasValue ? (pathBase + path).ToString() : "/";

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

        private string GetEncodedUrl(HttpRequest request)
        {
            return BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path, request.QueryString);
        }

        private async Task<HttpContent> MapContent(HttpRequest request)
        {
            if (request.Body == null)
            {
                return null;
            }

            var content = new ByteArrayContent(await ToByteArray(request.Body));

            content.Headers.TryAddWithoutValidation("Content-Type", new[] {request.ContentType});

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
    }
}