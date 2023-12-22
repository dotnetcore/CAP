// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace

#nullable enable
namespace DotNetCore.CAP;

/// <summary>
/// Represents all the option you can use to configure the dashboard.
/// </summary>
public class DashboardOptions
{
    public DashboardOptions()
    {
        PathBase = string.Empty;
        PathMatch = "/cap";
        StatsPollingInterval = 2000;
        AllowAnonymousExplicit = true;
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
    /// Explicitly allows anonymous access for the CAP dashboard API, passing AllowAnonymous to the ASP.NET Core global authorization filter.
    /// </summary>
    public bool AllowAnonymousExplicit { get; set; }
    
    /// <summary>
    /// Authorization policy for the Dashboard. Required if <see cref="AllowAnonymousExplicit"/> is false.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }
}