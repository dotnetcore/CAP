using System.Collections.Generic;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// Defines an interface for selecting an consumer service method to invoke for the current message.
    /// </summary>
    internal interface IConsumerServiceSelector
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
        /// <returns></returns>
        ConsumerExecutorDescriptor
            SelectBestCandidate(string key, IReadOnlyList<ConsumerExecutorDescriptor> candidates);
    }
}