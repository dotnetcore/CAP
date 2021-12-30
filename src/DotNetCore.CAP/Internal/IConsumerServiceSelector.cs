// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Defines an interface for selecting an consumer service method to invoke for the current message.
    /// </summary>
    public interface IConsumerServiceSelector
    {
        /// <summary>
        /// Selects a set of <see cref="ConsumerExecutorDescriptor" /> candidates for the current message associated with
        /// </summary>
        /// <returns>A set of <see cref="ConsumerExecutorDescriptor" /> candidates or <c>null</c>.</returns>
        IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates();

        /// <summary>
        /// Selects the best <see cref="ConsumerExecutorDescriptor" /> candidate from <paramref name="candidates" /> for the
        /// current message associated.
        /// </summary>
        /// <param name="key">topic or exchange router key.</param>
        /// <param name="candidates">the set of <see cref="ConsumerExecutorDescriptor" /> candidates.</param>
        ConsumerExecutorDescriptor? SelectBestCandidate(string key, IReadOnlyList<ConsumerExecutorDescriptor> candidates);
    }
}