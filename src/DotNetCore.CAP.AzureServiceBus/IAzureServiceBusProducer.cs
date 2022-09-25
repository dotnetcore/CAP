using System;
using System.Collections.Generic;
using Microsoft.Azure.ServiceBus;

namespace DotNetCore.CAP.AzureServiceBus;

public interface IAzureServiceBusProducer
{
    /// <summary>
    /// Topic to which the message will be produced.
    /// </summary>
    string TopicPath { get; }

    /// <summary>
    /// Type of the message that will be produced.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    /// Full name of <see cref="MessageType"/>.
    /// </summary>
    string MessageTypeFullName { get; }

    /// <summary>
    /// CustomHeaders to be added to the Message,
    /// </summary>
    Dictionary<string, string>? CustomHeaders { get; }


    bool EnableSessions { get; }


    RetryPolicy? RetryPolicy { get; }
}
