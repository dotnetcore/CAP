// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Perform user definition method of consumers.
    /// </summary>
    public interface ISubscribeInvoker
    {
        /// <summary>
        /// Invoke subscribe method with the consumer context.
        /// </summary>
        /// <param name="context">consumer execute context</param>
        /// <param name="cancellationToken">The object of <see cref="CancellationToken"/>.</param>
        Task<ConsumerExecutedResult> InvokeAsync(ConsumerContext context, CancellationToken cancellationToken = default);
    }
}