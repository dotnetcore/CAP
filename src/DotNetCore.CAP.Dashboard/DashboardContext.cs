// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Text.RegularExpressions;
using DotNetCore.CAP.Persistence;
using Microsoft.AspNetCore.Http;

namespace DotNetCore.CAP.Dashboard
{
    public abstract class DashboardContext
    {
        protected DashboardContext(IDataStorage storage, DashboardOptions options)
        {
            if (storage == null)
            {
                throw new ArgumentNullException(nameof(storage));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            Storage = storage;
            Options = options;
        }

        public IDataStorage Storage { get; }

        public DashboardOptions Options { get; }

        public Match UriMatch { get; set; }

        public DashboardRequest Request { get; protected set; }

        public DashboardResponse Response { get; protected set; }

        public IServiceProvider RequestServices { get; protected set; }
    }

    public sealed class CapDashboardContext : DashboardContext
    {
        public CapDashboardContext(
            IDataStorage storage,
            DashboardOptions options,
            HttpContext httpContext)
            : base(storage, options)
        {
            if (httpContext == null)
            {
                throw new ArgumentNullException(nameof(httpContext));
            }

            HttpContext = httpContext;
            Request = new CapDashboardRequest(httpContext);
            Response = new CapDashboardResponse(httpContext);
            RequestServices = httpContext.RequestServices;
        }

        public HttpContext HttpContext { get; }
    }
}