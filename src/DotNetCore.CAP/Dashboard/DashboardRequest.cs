// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace DotNetCore.CAP.Dashboard
{
    public abstract class DashboardRequest
    {
        public abstract string Method { get; }
        public abstract string Path { get; }
        public abstract string PathBase { get; }

        public abstract string LocalIpAddress { get; }
        public abstract string RemoteIpAddress { get; }

        public abstract string GetQuery(string key);

        public abstract Task<IList<string>> GetFormValuesAsync(string key);
    }

    internal sealed class CapDashboardRequest : DashboardRequest
    {
        private readonly HttpContext _context;

        public CapDashboardRequest(HttpContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context)); 
        }

        public override string Method => _context.Request.Method;
        public override string Path => _context.Request.Path.Value;
        public override string PathBase => _context.Request.PathBase.Value;
        public override string LocalIpAddress => _context.Connection.LocalIpAddress.MapToIPv4().ToString();
        public override string RemoteIpAddress => _context.Connection.RemoteIpAddress.MapToIPv4().ToString();

        public override string GetQuery(string key)
        {
            return _context.Request.Query[key];
        }

        public override async Task<IList<string>> GetFormValuesAsync(string key)
        {
            var form = await _context.Request.ReadFormAsync();
            return form[key];
        }
    }
}