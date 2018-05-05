// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotNetCore.CAP.Dashboard
{
    public abstract class DashboardResponse
    {
        public abstract string ContentType { get; set; }
        public abstract int StatusCode { get; set; }

        public abstract Stream Body { get; }

        public abstract void SetExpire(DateTimeOffset? value);

        public abstract Task WriteAsync(string text);
    }

    internal sealed class CapDashboardResponse : DashboardResponse
    {
        private readonly HttpContext _context;

        public CapDashboardResponse(HttpContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public override string ContentType
        {
            get => _context.Response.ContentType;
            set => _context.Response.ContentType = value;
        }

        public override int StatusCode
        {
            get => _context.Response.StatusCode;
            set => _context.Response.StatusCode = value;
        }

        public override Stream Body => _context.Response.Body;

        public override Task WriteAsync(string text)
        {
            return _context.Response.WriteAsync(text);
        }

        public override void SetExpire(DateTimeOffset? value)
        {
            _context.Response.Headers["Expires"] = value?.ToString("r", CultureInfo.InvariantCulture);
        }
    }
}