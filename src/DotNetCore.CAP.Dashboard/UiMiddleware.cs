using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
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

        public UiMiddleware(RequestDelegate next, IWebHostEnvironment hostingEnv, ILoggerFactory loggerFactory, DashboardOptions options)
        {
            _options = options ?? new DashboardOptions();

            _staticFileMiddleware = CreateStaticFileMiddleware(next, hostingEnv, loggerFactory, options);
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var httpMethod = httpContext.Request.Method;
            var path = httpContext.Request.Path.Value;


            if (httpMethod == "GET" && Regex.IsMatch(path, $"^/?{Regex.Escape(_options.PathMatch)}/?$", RegexOptions.IgnoreCase))
            {
                var redirectUrl = string.IsNullOrEmpty(path) || path.EndsWith("/") ? "index.html" : $"{path.Split('/').Last()}/index.html";

                httpContext.Response.StatusCode = 301;
                httpContext.Response.Headers["Location"] = redirectUrl;
                return;
            }

            if (httpMethod == "GET" && Regex.IsMatch(path, $"^/?{Regex.Escape(_options.PathMatch)}/?index.html$", RegexOptions.IgnoreCase))
            {
                var isAuthenticated = httpContext.User?.Identity?.IsAuthenticated;

                if (isAuthenticated == false && _options.UseChallengeOnAuth)
                {
                    await httpContext.ChallengeAsync(_options.DefaultChallengeScheme);
                    return;
                }

                httpContext.Response.StatusCode = 200;
                httpContext.Response.ContentType = "text/html;charset=utf-8";

                await using var stream = GetType().Assembly.GetManifestResourceStream(EmbeddedFileNamespace + ".index.html");
                if (stream == null) throw new InvalidOperationException();

                using var sr = new StreamReader(stream);
                var htmlBuilder = new StringBuilder(await sr.ReadToEndAsync());
                htmlBuilder.Replace("%(servicePrefix)", _options.PathMatch + "/api");
                htmlBuilder.Replace("%(pollingInterval)", _options.StatsPollingInterval.ToString());
                await httpContext.Response.WriteAsync(htmlBuilder.ToString(), Encoding.UTF8);

                return;
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
