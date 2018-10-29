// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;

namespace DotNetCore.CAP.Dashboard
{
    public static class RouteCollectionExtensions
    {
        public static void AddRazorPage(
            this RouteCollection routes,
            string pathTemplate,
            Func<Match, RazorPage> pageFunc)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (pathTemplate == null)
            {
                throw new ArgumentNullException(nameof(pathTemplate));
            }

            if (pageFunc == null)
            {
                throw new ArgumentNullException(nameof(pageFunc));
            }

            routes.Add(pathTemplate, new RazorPageDispatcher(pageFunc));
        }

        public static void AddCommand(
            this RouteCollection routes,
            string pathTemplate,
            Func<DashboardContext, bool> command)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (pathTemplate == null)
            {
                throw new ArgumentNullException(nameof(pathTemplate));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            routes.Add(pathTemplate, new CommandDispatcher(command));
        }

        public static void AddJsonResult(
            this RouteCollection routes,
            string pathTemplate,
            Func<DashboardContext, object> func)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (pathTemplate == null)
            {
                throw new ArgumentNullException(nameof(pathTemplate));
            }

            if (func == null)
            {
                throw new ArgumentNullException(nameof(func));
            }

            routes.Add(pathTemplate, new JsonDispatcher(func));
        }

        public static void AddJsonResult(
            this RouteCollection routes,
            string pathTemplate,
            Func<DashboardContext, string> jsonfunc)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (pathTemplate == null)
            {
                throw new ArgumentNullException(nameof(pathTemplate));
            }

            if (jsonfunc == null)
            {
                throw new ArgumentNullException(nameof(jsonfunc));
            }

            routes.Add(pathTemplate, new JsonDispatcher(jsonfunc));
        }

        public static void AddPublishBatchCommand(
            this RouteCollection routes,
            string pathTemplate,
            Action<DashboardContext, long> command)
        {
            if (routes == null)
            {
                throw new ArgumentNullException(nameof(routes));
            }

            if (pathTemplate == null)
            {
                throw new ArgumentNullException(nameof(pathTemplate));
            }

            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            routes.Add(pathTemplate, new BatchCommandDispatcher(command));
        }
    }
}