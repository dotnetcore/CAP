// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Dashboard
{
    public class UiMiddleware
    {
        private const string EmbeddedFileNamespace = "DotNetCore.CAP.Dashboard.wwwroot.dist";

        private readonly DashboardOptions _options;
        private readonly StaticFileMiddleware _staticFileMiddleware;
        private readonly Regex _redirectUrlCheckRegex;
        private readonly Regex _homeUrlCheckRegex;

        public UiMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, ILoggerFactory loggerFactory, DashboardOptions options)
        {
            _options = options ?? new DashboardOptions();

            _staticFileMiddleware = CreateStaticFileMiddleware(next, hostingEnv, loggerFactory, options);

            _redirectUrlCheckRegex = new Regex($"^/?{Regex.Escape(_options.PathMatch)}/?$", RegexOptions.IgnoreCase);
            _homeUrlCheckRegex = new Regex($"^/?{Regex.Escape(_options.PathMatch)}/?index.html$", RegexOptions.IgnoreCase);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var httpMethod = httpContext.Request.Method;
            var path = httpContext.Request.Path.Value;

            if (httpMethod == "GET")
            {
                if (_redirectUrlCheckRegex.IsMatch(path))
                {
                    var redirectUrl = string.IsNullOrEmpty(path) || path.EndsWith("/") ? "index.html" : $"{path.Split('/').Last()}/index.html";

                    httpContext.Response.StatusCode = 301;
                    httpContext.Response.Headers["Location"] = redirectUrl;
                    return;
                }

                if (_homeUrlCheckRegex.IsMatch(path))
                {
                    if (!await CapBuilderExtension.Authentication(httpContext, _options))
                    {
                        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        return;
                    }

                    httpContext.Response.StatusCode = 200;
                    httpContext.Response.ContentType = "text/html;charset=utf-8";

                    await using var stream = GetType().Assembly.GetManifestResourceStream(EmbeddedFileNamespace + ".index.html");
                    if (stream == null) throw new InvalidOperationException();

                    using var sr = new StreamReader(stream);
                    var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
                    htmlBuilder.Replace("%(servicePrefix)", _options.PathBase + _options.PathMatch + "/api");
                    htmlBuilder.Replace("%(pollingInterval)", _options.StatsPollingInterval.ToString());
                    await httpContext.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);

                    return;
                }
            }

            await _staticFileMiddleware.Invoke(httpContext);
        }

        private StaticFileMiddleware CreateStaticFileMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, ILoggerFactory loggerFactory, DashboardOptions options)
        {
            var staticFileOptions = new StaticFileOptions
            {
                RequestPath = options.PathMatch,
                FileProvider = new EmbeddedFileProvider(typeof(UiMiddleware).GetTypeInfo().Assembly, EmbeddedFileNamespace),
            };

            return new StaticFileMiddleware(next, hostingEnv, Options.Create(staticFileOptions), loggerFactory);
        }
    }
}
