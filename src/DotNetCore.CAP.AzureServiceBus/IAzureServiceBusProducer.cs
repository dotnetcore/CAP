using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus;

public interface IAzureServiceBusProducer
{
    string ConnectionString { get; }
    string TopicPath { get; }
    Type MessageType { get; }
    string MessageTypeFullName { get; }
    Dictionary<string, string>? CustomHeaders { get; }
    bool EnableSessions { get; }
    RetryPolicy RetryPolicy { get; }
}
