// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace

using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP.Filter;

/// <summary>
/// A filter that surrounds execution of the subscriber.
/// </summary>
public interface ISubscribeFilter
{
    /// <summary>
    /// Called before the subscriber executes.
    /// </summary>
    /// <param name="context">The <see cref="ExecutingContext" />.</param>
    Task OnSubscribeExecutingAsync(ExecutingContext context);

    /// <summary>
    /// Called after the subscriber executes.
    /// </summary>
    /// <param name="context">The <see cref="ExecutedContext" />.</param>
    Task OnSubscribeExecutedAsync(ExecutedContext context);

    /// <summary>
    /// Called after the subscriber has thrown an <see cref="System.Exception" />.
    /// </summary>
    /// <param name="context">The <see cref="ExceptionContext" />.</param>
    Task OnSubscribeExceptionAsync(ExceptionContext context);
}