// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP
{
    public class DashboardOptions
    {
        public DashboardOptions()
        {
            PathMatch = "/cap";
            StatsPollingInterval = 2000;
        }

        public string PathMatch { get; set; }
         
        /// <summary>
        /// The interval the /stats endpoint should be polled with.
        /// </summary>
        public int StatsPollingInterval { get; set; }

        public bool UseChallengeOnAuth { get; set; }

        /// <summary>
        /// Default ChallengeScheme used for Dashboard authentication. If no scheme is set, the DefaultScheme set up in AddAuthentication will be used.
        /// </summary>
        public string DefaultChallengeScheme { get; set; }
    }
}