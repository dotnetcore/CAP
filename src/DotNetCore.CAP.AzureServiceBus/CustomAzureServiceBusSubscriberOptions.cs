using System;
using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus;

public class CustomAzureServiceBusSubscriberOptions
{
    public Type MessageType { get; set; }

    public string? ConnectionString { get; set; }
        
    public bool EnableSessions { get; set; } = false;
        
    public string TopicPath { get; protected set; }

    public string? SubscriptionName { get; set; }

    public RetryPolicy? RetryPolicy { get; set; }

    public ReceiveMode? ReceiveMode { get; set; }
}