using System;
using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus;

internal sealed class AzureServiceBusSubscriptionConfigurator
{
    public AzureServiceBusSubscriptionConfigurator(string topicPath, string? subscriptionName)
    {
        Default = true;
        TopicPath = topicPath;
        SubscriptionName = subscriptionName;
    }

    public AzureServiceBusSubscriptionConfigurator(Type messageType, string topicPath, string? subscriptionName,
        RetryPolicy? retryPolicy, ReceiveMode? receiveMode)
    {
        Default = false;
        MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        TopicPath = topicPath;
        SubscriptionName = subscriptionName;
        RetryPolicy = retryPolicy;
        ReceiveMode = receiveMode;
    }

    public AzureServiceBusSubscriptionConfigurator(CustomAzureServiceBusSubscriberOptions options)
    {
        Default = false;
        MessageType = options.MessageType;
        TopicPath = options.TopicPath;
        SubscriptionName = options.SubscriptionName;
        RetryPolicy = options.RetryPolicy;
        ReceiveMode = options.ReceiveMode;
        EnableSessions = options.EnableSessions;
    }

    public bool EnableSessions { get; set; }

    public bool Default { get; set; }

    public Type? MessageType { get; set; }

    public string TopicPath { get; protected set; }

    public string? SubscriptionName { get; set; }

    public RetryPolicy? RetryPolicy { get; set; }

    public ReceiveMode? ReceiveMode { get; set; }

    public override bool Equals(object obj)
        => obj is AzureServiceBusSubscriptionConfigurator configurator
           && configurator.MessageType! == this.MessageType!
           && configurator.TopicPath == this.TopicPath;
    
    public override int GetHashCode()
    {
        unchecked
        {
            var result = 0;
            result = (result * 397) ^ MessageType.GetHashCode();
            result = (result * 397) ^ TopicPath.GetHashCode();
            return result;
        }
    }
}