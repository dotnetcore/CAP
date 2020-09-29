﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace DotNetCore.CAP.Messages
{
    public class Message
    {
        /// <summary>
        /// System.Text.Json requires that class explicitly has a parameterless constructor
        /// and public properties have a setter.
        /// </summary>
        public Message() { }

        public Message(IDictionary<string, string> headers, [CanBeNull] object value)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Value = value;
        }

        public IDictionary<string, string> Headers { get; set; }

        [CanBeNull]
        public object Value { get; set; }
    }

    public static class MessageExtensions
    {
        public static string GetId(this Message message)
        {
            message.Headers.TryGetValue(Headers.MessageId, out var value);
            return value;
        }

        public static string GetName(this Message message)
        {
            message.Headers.TryGetValue(Headers.MessageName, out var value);
            return value;
        }

        public static string GetCallbackName(this Message message)
        {
            message.Headers.TryGetValue(Headers.CallbackName, out var value);
            return value;
        }

        public static string GetGroup(this Message message)
        {
            message.Headers.TryGetValue(Headers.Group, out var value);
            return value;
        }

        public static int GetCorrelationSequence(this Message message)
        {
            if (message.Headers.TryGetValue(Headers.CorrelationSequence, out var value))
            {
                return int.Parse(value);
            }

            return 0;
        }

        public static bool HasException(this Message message)
        {
            return message.Headers.ContainsKey(Headers.Exception);
        }

        public static void AddOrUpdateException(this Message message, Exception ex)
        {
            var msg = $"{ex.GetType().Name}-->{ex.Message}";

            message.Headers[Headers.Exception] = msg;
        }

        public static void RemoveException(this Message message)
        {
            message.Headers.Remove(Headers.Exception);
        }
    }
}
