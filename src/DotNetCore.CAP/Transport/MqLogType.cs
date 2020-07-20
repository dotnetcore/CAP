// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Transport
{
    public enum MqLogType
    {
        //RabbitMQ
        ConsumerCancelled,
        ConsumerRegistered,
        ConsumerUnregistered,
        ConsumerShutdown,

        //Kafka
        ConsumeError,
        ServerConnError,

        //AzureServiceBus
        ExceptionReceived,

        //Amazon SQS
        InvalidIdFormat,
        MessageNotInflight
    }

    public class LogMessageEventArgs : EventArgs
    {
        public string Reason { get; set; }

        public MqLogType LogType { get; set; }
    }
}