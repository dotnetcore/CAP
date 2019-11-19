// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Messages;
using JetBrains.Annotations;

namespace DotNetCore.CAP.Serialization
{
    public interface ISerializer
    {
        /// <summary>
        /// Serializes the given <see cref="Message"/> into a <see cref="TransportMessage"/>
        /// </summary>
        Task<TransportMessage> SerializeAsync(Message message);

        /// <summary>
        /// Deserializes the given <see cref="TransportMessage"/> back into a <see cref="Message"/>
        /// </summary>
        Task<Message> DeserializeAsync(TransportMessage transportMessage, [CanBeNull] Type valueType);
    }
}