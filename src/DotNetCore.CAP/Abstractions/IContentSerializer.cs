// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using DotNetCore.CAP.Models;

namespace DotNetCore.CAP.Abstractions
{
    /// <summary>
    /// Message content serializer.
    /// <para>
    /// By default, CAP will use Json as a serializer, and you can customize this interface to achieve serialization of
    /// other methods.
    /// </para>
    /// </summary>
    public interface IContentSerializer
    {
        /// <summary>
        /// Serializes the specified object to a string.
        /// </summary>
        /// <typeparam name="T"> The type of the value being serialized.</typeparam>
        /// <param name="value">The object to serialize.</param>
        /// <returns>A string representation of the object.</returns>
        string Serialize<T>(T value);

        /// <summary>
        /// Deserializes the string to the specified .NET type.
        /// </summary>
        /// <typeparam name="T">The type of the object to deserialize to.</typeparam>
        /// <param name="value">The content string to deserialize.</param>
        /// <returns>The deserialized object from the string.</returns>
        T DeSerialize<T>(string value);

        /// <summary>
        /// Deserializes the string to the specified .NET type.
        /// </summary>
        /// <param name="value">The string to deserialize.</param>
        /// <param name="type">The type of the object to deserialize to.</param>
        /// <returns>The deserialized object from the string.</returns>
        object DeSerialize(string value, Type type);
    }

    /// <summary>
    /// CAP message content wapper.
    /// <para>You can customize the message body filed name of the wrapper or add fields that you interested.</para>
    /// </summary>
    /// <remarks>
    /// We use the wrapper to provide some additional information for the message content,which is important for CAP。
    /// Typically, we may need to customize the field display name of the message,
    /// which includes interacting with other message components, which can be adapted in this manner
    /// </remarks>
    public interface IMessagePacker
    {
        /// <summary>
        /// Package a message object
        /// </summary>
        /// <param name="obj">The obj message to be packed.</param>
        string Pack(CapMessage obj);

        /// <summary>
        /// Unpack a message strings to <see cref="CapMessage" /> object.
        /// </summary>
        /// <param name="packingMessage">The string of packed message.</param>
        CapMessage UnPack(string packingMessage);
    }
}