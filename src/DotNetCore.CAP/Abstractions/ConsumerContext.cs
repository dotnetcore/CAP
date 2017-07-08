using System;
using DotNetCore.CAP.Infrastructure;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// A context for consumers, it used to be provider wapper of method description and received message.
    /// </summary>
    public class ConsumerContext
    {
        /// <summary>
        /// create a new instance of  <see cref="ConsumerContext"/> .
        /// </summary>
        /// <param name="descriptor">consumer method descriptor. </param>
        /// <param name="message"> reveied message.</param>
        public ConsumerContext(ConsumerExecutorDescriptor descriptor, MessageContext message)
        {
            ConsumerDescriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            DeliverMessage = message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// a descriptor of consumer information need to be performed.
        /// </summary>
        public ConsumerExecutorDescriptor ConsumerDescriptor { get; set; }

        /// <summary>
        /// consumer reveived message.
        /// </summary>
        public MessageContext DeliverMessage { get; set; }
    }
}