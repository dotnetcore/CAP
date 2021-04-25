// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Messages;

namespace DotNetCore.CAP.Internal
{
    /// <summary>
    /// A context for consumers, it used to be provider wrapper of method description and received message.
    /// </summary>
    public class ConsumerContext
    {
        /// <summary>
        /// create a new instance of  <see cref="ConsumerContext" /> .
        /// </summary>
        /// <param name="descriptor">consumer method descriptor. </param>
        /// <param name="message"> received message.</param>
        public ConsumerContext(ConsumerExecutorDescriptor descriptor, ICapMessage message)
        {
            ConsumerDescriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            DeliverMessage = (IMessage)message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// a descriptor of consumer information need to be performed.
        /// </summary>
        public ConsumerExecutorDescriptor ConsumerDescriptor { get; }

        /// <summary>
        /// consumer received message.
        /// </summary>
        public IMessage DeliverMessage { get; }
    }


    /// <summary>
    /// A context for consumers, it used to be provider wrapper of method description and received message.
    /// </summary>
    public class ConsumerContext<T>
    {
        /// <summary>
        /// create a new instance of  <see cref="ConsumerContext<T>" /> .
        /// </summary>
        /// <param name="descriptor">consumer method descriptor. </param>
        /// <param name="message"> received message.</param>
        public ConsumerContext(ConsumerExecutorDescriptor descriptor, ICapMessage message)
        {
            ConsumerDescriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            DeliverMessage = (IMessage<T>)message ?? throw new ArgumentNullException(nameof(message));
        }

        /// <summary>
        /// a descriptor of consumer information need to be performed.
        /// </summary>
        public ConsumerExecutorDescriptor ConsumerDescriptor { get; }

        /// <summary>
        /// consumer received message.
        /// </summary>
        public IMessage<T> DeliverMessage { get; }
    }
}