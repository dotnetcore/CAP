// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using DotNetCore.CAP.Dashboard;
using Microsoft.AspNetCore.Authentication.Cookies;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class DashboardOptions
    {
        public DashboardOptions()
        {
            AppPath = "/";
            PathMatch = "/cap";
            Authorization = new[] {new LocalRequestsOnlyAuthorizationFilter()};
            StatsPollingInterval = 2000;
            UseChallengeOnAuth = false;
        }

        /// <summary>
        /// Default ChallengeScheme used for Dashboard authentication. If no scheme is set, the DefaultScheme set up in AddAuthentication will be used.
        /// </summary>
        public string DefaultChallengeScheme { get; set; }
        
        /// <summary>
        /// Indicates if executes a Challenge for Auth within ASP.NET middlewares
        /// </summary>
        public bool UseChallengeOnAuth { get; set; }

        /// <summary>
        /// The path for the Back To Site link. Set to <see langword="null" /> in order to hide the Back To Site link.
        /// </summary>
        public string AppPath { get; set; }

        public string PathMatch { get; set; }

        public IEnumerable<IDashboardAuthorizationFilter> Authorization { get; set; }

        /// <summary>
        /// The interval the /stats endpoint should be polled with.
        /// </summary>
        public int StatsPollingInterval { get; set; }
    }
}