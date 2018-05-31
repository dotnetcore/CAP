// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;

namespace DotNetCore.CAP.Diagnostics
{
    public class BrokerEventData : EventData
    {
        public BrokerEventData(Guid operationId, string operation, string brokerAddress,
            string brokerTopicName, string brokerTopicBody)
            : base(operationId, operation)
        {
            BrokerAddress = brokerAddress;
            BrokerTopicName = brokerTopicName;
            BrokerTopicBody = brokerTopicBody;
        }

        public TracingHeaders Headers { get; set; }

        public string BrokerAddress { get; set; }

        public string BrokerTopicBody { get; set; }

        public string BrokerTopicName { get; set; }
    }
}