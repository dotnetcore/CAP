using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.Messages
{
    public interface ICapMessage
    {
        public IDictionary<string, string> Headers { get; set; }
    }
    public interface IMessage: ICapMessage
    {
        public object Value { get; set; }

    }

    public interface IMessage<T>: ICapMessage
    {
        public T Value { get; set; }

    }

    public static class IMessageExtensions
    {
        public static string GetId(this ICapMessage message)
        {
            message.Headers.TryGetValue(Headers.MessageId, out var value);
            return value;
        }

        public static string GetName(this ICapMessage message)
        {
            message.Headers.TryGetValue(Headers.MessageName, out var value);
            return value;
        }

        public static string GetCallbackName(this ICapMessage message)
        {
            message.Headers.TryGetValue(Headers.CallbackName, out var value);
            return value;
        }

        public static string GetGroup(this ICapMessage message)
        {
            message.Headers.TryGetValue(Headers.Group, out var value);
            return value;
        }

        public static int GetCorrelationSequence(this ICapMessage message)
        {
            if (message.Headers.TryGetValue(Headers.CorrelationSequence, out var value))
            {
                return int.Parse(value);
            }

            return 0;
        }

        public static bool HasException(this ICapMessage message)
        {
            return message.Headers.ContainsKey(Headers.Exception);
        }

        public static void AddOrUpdateException(this ICapMessage message, Exception ex)
        {
            var msg = $"{ex.GetType().Name}-->{ex.Message}";

            message.Headers[Headers.Exception] = msg;
        }

        public static void RemoveException(this ICapMessage message)
        {
            message.Headers.Remove(Headers.Exception);
        }

        public static Type GetMessageType(this ICapMessage message)
        {
            message.Headers.TryGetValue(Headers.Type, out var value);

            return Type.GetType(value);

        }

        public static string GetId(this IMessage message)
        {
            message.Headers.TryGetValue(Headers.MessageId, out var value);
            return value;
        }

        public static string GetName(this IMessage message)
        {
            message.Headers.TryGetValue(Headers.MessageName, out var value);
            return value;
        }

        public static string GetCallbackName(this IMessage message)
        {
            message.Headers.TryGetValue(Headers.CallbackName, out var value);
            return value;
        }

        public static string GetGroup(this IMessage message)
        {
            message.Headers.TryGetValue(Headers.Group, out var value);
            return value;
        }

        public static int GetCorrelationSequence(this IMessage message)
        {
            if (message.Headers.TryGetValue(Headers.CorrelationSequence, out var value))
            {
                return int.Parse(value);
            }

            return 0;
        }

        public static bool HasException(this IMessage message)
        {
            return message.Headers.ContainsKey(Headers.Exception);
        }

        public static void AddOrUpdateException(this IMessage message, Exception ex)
        {
            var msg = $"{ex.GetType().Name}-->{ex.Message}";

            message.Headers[Headers.Exception] = msg;
        }

        public static void RemoveException(this IMessage message)
        {
            message.Headers.Remove(Headers.Exception);
        }

        public static Type GetMessageType(this IMessage message)
        {
            message.Headers.TryGetValue(Headers.Type, out var value);

            return Type.GetType(value);

        }

        public static string GetId<T>(this IMessage<T> message)
        {
            message.Headers.TryGetValue(Headers.MessageId, out var value);
            return value;
        }

        public static string GetName<T>(this IMessage<T> message)
        {
            message.Headers.TryGetValue(Headers.MessageName, out var value);
            return value;
        }

        public static string GetCallbackName<T>(this IMessage<T> message)
        {
            message.Headers.TryGetValue(Headers.CallbackName, out var value);
            return value;
        }

        public static string GetGroup<T>(this IMessage<T> message)
        {
            message.Headers.TryGetValue(Headers.Group, out var value);
            return value;
        }

        public static int GetCorrelationSequence<T>(this IMessage<T> message)
        {
            if (message.Headers.TryGetValue(Headers.CorrelationSequence, out var value))
            {
                return int.Parse(value);
            }

            return 0;
        }

        public static bool HasException<T>(this IMessage<T> message)
        {
            return message.Headers.ContainsKey(Headers.Exception);
        }

        public static void AddOrUpdateException<T>(this IMessage<T> message, Exception ex)
        {
            var msg = $"{ex.GetType().Name}-->{ex.Message}";

            message.Headers[Headers.Exception] = msg;
        }

        public static void RemoveException<T>(this IMessage<T> message)
        {
            message.Headers.Remove(Headers.Exception);
        }

        public static Type GetMessageType<T>(this IMessage<T> message)
        {
            message.Headers.TryGetValue(Headers.Type, out var value);

            return Type.GetType(value);

        }


    }


}
