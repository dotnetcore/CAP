// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace

namespace DotNetCore.CAP;

/// <summary>
/// Represents all the option you can use to configure the dashboard.
/// </summary>
public class DashboardOptions
{
    public DashboardOptions()
    {
        PathMatch = "/cap";
        StatsPollingInterval = 2000;
        AllowAnonymousExplicit = false;
    }

    /// <summary>
    /// When behind the proxy, specify the base path to allow spa call prefix.
    /// </summary>
    public string PathBase { get; set; }

    /// <summary>
    /// Path prefix to match from url path.
    /// </summary>
    public string PathMatch { get; set; }

    /// <summary>
    /// The interval the /stats endpoint should be polled with.
    /// </summary>
    public int StatsPollingInterval { get; set; }

    /// <summary>
    /// Allow Explicit to set AllowAnonymous for the CAP dashboard API without use ASP.NET Core global authorization filter.
    /// Default true
    /// </summary>
    public bool AllowAnonymousExplicit { get; set; }
    
    /// <summary>
    /// Authorization policy for the Dashboard. Required if <see cref="AllowAnonymousExplicit"/> is false.
    /// </summary>
    public string AuthorizationPolicy { get; set; }
}