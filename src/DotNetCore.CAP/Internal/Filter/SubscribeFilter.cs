// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

// ReSharper disable once CheckNamespace

using System.Threading.Tasks;

// ReSharper disable once CheckNamespace
namespace DotNetCore.CAP.Filter;

/// <summary>
/// Abstract base class for ISubscribeFilter for use when implementing a subset of the interface methods.
/// </summary>
public abstract class SubscribeFilter : ISubscribeFilter
{
    /// <summary>
    /// Called before the subscriber executes.
    /// </summary>
    /// <param name="context">The <see cref="ExecutingContext" />.</param>
    public virtual Task OnSubscribeExecutingAsync(ExecutingContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after the subscriber executes.
    /// </summary>
    /// <param name="context">The <see cref="ExecutedContext" />.</param>
    public virtual Task OnSubscribeExecutedAsync(ExecutedContext context)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called after the subscriber has thrown an <see cref="System.Exception" />.
    /// </summary>
    /// <param name="context">The <see cref="ExceptionContext" />.</param>
    public virtual Task OnSubscribeExceptionAsync(ExceptionContext context)
    {
        return Task.CompletedTask;
    }


}