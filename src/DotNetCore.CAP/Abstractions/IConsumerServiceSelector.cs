using System;
using System.Collections.Generic;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// Defines an interface for selecting an cosumer service method to invoke for the current message.
    /// </summary>
    public interface IConsumerServiceSelector
    {
        /// <summary>
        /// Selects a set of <see cref="ConsumerExecutorDescriptor"/> candidates for the current message associated with
        /// <paramref name="provider"/>.
        /// </summary>
        /// <param name="provider"> <see cref="IServiceProvider"/>.</param>
        /// <returns>A set of <see cref="ConsumerExecutorDescriptor"/> candidates or <c>null</c>.</returns>
        IReadOnlyList<ConsumerExecutorDescriptor> SelectCandidates(IServiceProvider provider);

        /// <summary>
        /// Selects the best <see cref="ConsumerExecutorDescriptor"/> candidate from <paramref name="candidates"/> for the
        /// current message associated.
        /// </summary>
        /// <param name="key">topic or exchange router key.</param>
        /// <param name="candidates">the set of <see cref="ConsumerExecutorDescriptor"/> candidates.</param>
        /// <returns></returns>
        ConsumerExecutorDescriptor
            SelectBestCandidate(string key, IReadOnlyList<ConsumerExecutorDescriptor> candidates);
    }
}