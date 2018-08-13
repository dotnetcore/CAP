// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Reflection;
using DotNetCore.CAP.Dashboard.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetCore.CAP.Dashboard
{
    public static class DashboardRoutes
    {
        private static readonly string[] Javascripts =
        {
            "jquery-2.1.4.min.js",
            "bootstrap.min.js",
            "moment.min.js",
            "moment-with-locales.min.js",
            "d3.min.js",
            "d3.layout.min.js",
            "rickshaw.min.js",
            "jsonview.min.js",
            "cap.js"
        };

        private static readonly string[] Stylesheets =
        {
            "bootstrap.min.css",
            "rickshaw.min.css",
            "jsonview.min.css",
            "cap.css"
        };

        static DashboardRoutes()
        {
            Routes = new RouteCollection();
            Routes.AddRazorPage("/", x => new HomePage());
            Routes.Add("/stats", new JsonStats());
            Routes.Add("/health", new OkStats());

            #region Embedded static content

            Routes.Add("/js[0-9]+", new CombinedResourceDispatcher(
                "application/javascript",
                GetExecutingAssembly(),
                GetContentFolderNamespace("js"),
                Javascripts));

            Routes.Add("/css[0-9]+", new CombinedResourceDispatcher(
                "text/css",
                GetExecutingAssembly(),
                GetContentFolderNamespace("css"),
                Stylesheets));

            Routes.Add("/fonts/glyphicons-halflings-regular/eot", new EmbeddedResourceDispatcher(
                "application/vnd.ms-fontobject",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.eot")));

            Routes.Add("/fonts/glyphicons-halflings-regular/svg", new EmbeddedResourceDispatcher(
                "image/svg+xml",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.svg")));

            Routes.Add("/fonts/glyphicons-halflings-regular/ttf", new EmbeddedResourceDispatcher(
                "application/octet-stream",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.ttf")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff", new EmbeddedResourceDispatcher(
                "font/woff",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff")));

            Routes.Add("/fonts/glyphicons-halflings-regular/woff2", new EmbeddedResourceDispatcher(
                "font/woff2",
                GetExecutingAssembly(),
                GetContentResourceName("fonts", "glyphicons-halflings-regular.woff2")));

            #endregion Embedded static content

            #region Razor pages and commands

            Routes.AddJsonResult("/published/message/(?<Id>.+)", x =>
            {
                var id = long.Parse(x.UriMatch.Groups["Id"].Value);
                var message = x.Storage.GetConnection().GetPublishedMessageAsync(id)
                    .GetAwaiter().GetResult();
                return message.Content;
            });
            Routes.AddJsonResult("/received/message/(?<Id>.+)", x =>
            {
                var id = long.Parse(x.UriMatch.Groups["Id"].Value);
                var message = x.Storage.GetConnection().GetReceivedMessageAsync(id)
                    .GetAwaiter().GetResult();
                return message.Content;
            });

            Routes.AddPublishBatchCommand(
                "/published/requeue",
                (client, messageId) =>
                {
                    var msg = client.Storage.GetConnection().GetPublishedMessageAsync(messageId)
                        .GetAwaiter().GetResult(); 
                    client.RequestServices.GetService<IDispatcher>().EnqueueToPublish(msg);
                });
            Routes.AddPublishBatchCommand(
                "/received/requeue",
                (client, messageId) =>
                {
                    var msg = client.Storage.GetConnection().GetReceivedMessageAsync(messageId)
                        .GetAwaiter().GetResult();
                    client.RequestServices.GetService<IDispatcher>().EnqueueToExecute(msg);
                }); 

            Routes.AddRazorPage(
                "/published/(?<StatusName>.+)",
                x => new PublishedPage(x.Groups["StatusName"].Value));
            Routes.AddRazorPage(
                "/received/(?<StatusName>.+)",
                x => new ReceivedPage(x.Groups["StatusName"].Value));

            Routes.AddRazorPage("/subscribers", x => new SubscriberPage());

            Routes.AddRazorPage("/nodes", x => new NodePage());

            Routes.AddRazorPage("/nodes/node/(?<Id>.+)", x => new NodePage(x.Groups["Id"].Value));

            #endregion Razor pages and commands
        }

        public static RouteCollection Routes { get; }

        internal static string GetContentFolderNamespace(string contentFolder)
        {
            return $"{typeof(DashboardRoutes).Namespace}.Content.{contentFolder}";
        }

        internal static string GetContentResourceName(string contentFolder, string resourceName)
        {
            return $"{GetContentFolderNamespace(contentFolder)}.{resourceName}";
        }

        private static Assembly GetExecutingAssembly()
        {
            return typeof(DashboardRoutes).GetTypeInfo().Assembly;
        }
    }
}