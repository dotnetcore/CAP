using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus;

public sealed class AzureServiceBusProducer : IAzureServiceBusProducer
{
    public AzureServiceBusProducer(
        Type type,
        string topicPath,
        bool enableSessions,
        RetryPolicy retryPolicy = null,
        Dictionary<string, string>? customHeaders = null)
    {
        MessageType = type;
        TopicPath = topicPath;
        CustomHeaders = customHeaders;
        EnableSessions = enableSessions;
        RetryPolicy = retryPolicy;
    }

    public string TopicPath { get; }
    public Type MessageType { get; }
    public string MessageTypeFullName => MessageType.FullName;
    public Dictionary<string, string>? CustomHeaders { get; }
    public bool EnableSessions { get; }
    public RetryPolicy RetryPolicy { get; }
}

public sealed class AzureServiceBusProducer<T> : IAzureServiceBusProducer
{
    public AzureServiceBusProducer(
        string topicPath,
        bool enableSessions,
        RetryPolicy? retryPolicy = null,
        Dictionary<string, string>? customHeaders = null)
    {
        MessageType = typeof(T);
        TopicPath = topicPath;
        CustomHeaders = customHeaders;
        EnableSessions = enableSessions;
        RetryPolicy = retryPolicy;
    }

    public string TopicPath { get; }
    public Type MessageType { get; }
    public string MessageTypeFullName => MessageType.FullName;
    public Dictionary<string, string>? CustomHeaders { get; }
    public bool EnableSessions { get; }
    public RetryPolicy? RetryPolicy { get; }
}
