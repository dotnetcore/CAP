// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure.Core;

namespace DotNetCore.CAP.AzureServiceBus.Consumer;

public interface IServiceBusConsumerDescriptor
{
    string TopicPath { get; }
    
    string Namespace { get; }
    
    string ConnectionString { get; }
    
    TokenCredential? TokenCredential { get; }
    
     public AzureServiceBusOptions Options { get; }
}

public class ServiceBusConsumerDescriptor : IServiceBusConsumerDescriptor
{
    public ServiceBusConsumerDescriptor(string topicPath, string @namespace, string connectionString, TokenCredential? tokenCredential, AzureServiceBusOptions options)
    {
        TopicPath = topicPath;
        Namespace = @namespace;
        ConnectionString = connectionString;
        TokenCredential = tokenCredential;
        Options = options;
    }

    public string TopicPath { get; }
    
    public string Namespace { get; }
    
    public string ConnectionString { get; }
    
    public TokenCredential? TokenCredential { get; }
    
    public AzureServiceBusOptions Options { get; }
}
