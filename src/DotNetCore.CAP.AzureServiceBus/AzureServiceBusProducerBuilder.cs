using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus;

public class AzureServiceBusProducerBuilder<T>
{
    private string TopicPath { get; set; }
    private string ConnectionString { get; set; }
    private RetryPolicy? RetryPolicy { get; set; }

    private bool EnableSessions { get; set; }
    private Dictionary<string, string> CustomHeaders { get; set; } = new();

    public AzureServiceBusProducerBuilder<T> To(string key)
    {
        TopicPath = key;
        return this;
    }

    public AzureServiceBusProducerBuilder<T> WithConnectionString(string connectionString)
    {
        ConnectionString = connectionString;

        return this;
    }

    public AzureServiceBusProducerBuilder<T> WithRetryPolicy(RetryPolicy retryPolicy)
    {
        RetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
        return this;
    }

    public AzureServiceBusProducerBuilder<T> WithSessionEnabled()
    {
        EnableSessions = true;
        return this;
    }

    public AzureServiceBusProducerBuilder<T> WithCustomHeader(string key, string value)
    {
        CustomHeaders.Add(key, value);

        return this;
    }

    public AzureServiceBusProducer<T> Build()
    {
        return new AzureServiceBusProducer<T>(ConnectionString, TopicPath, EnableSessions, RetryPolicy, CustomHeaders);
    }
}
