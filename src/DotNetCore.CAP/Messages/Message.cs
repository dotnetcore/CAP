// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace DotNetCore.CAP.Messages
{
    public class Message
    {
        /// <summary>
        /// System.Text.Json requires that class explicitly has a parameter less constructor
        /// and public properties have a setter.
        /// </summary>
        public Message()
        {
            Headers = new Dictionary<string, string?>();
        }

        public Message(IDictionary<string, string?> headers, object? value)
        {
            Headers = headers ?? throw new ArgumentNullException(nameof(headers));
            Value = value;
        }

        public IDictionary<string, string?> Headers { get; set; }

        public object? Value { get; set; }
    }

    public static class MessageExtensions
    {
        public static string GetId(this Message message)
        {
            return message.Headers[Headers.MessageId]!;
        }

        public static string GetName(this Message message)
        {
            return message.Headers[Headers.MessageName]!;
        }

        public static string? GetCallbackName(this Message message)
        {
            message.Headers.TryGetValue(Headers.CallbackName, out var value);
            return value;
        }

        public static string? GetGroup(this Message message)
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

        public static string? GetExecutionInstanceId(this Message message)
        {
            message.Headers.TryGetValue(Headers.ExecutionInstanceId, out var value);
            return value;
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
