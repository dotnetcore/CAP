using DotNetCore.CAP;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.MQTT
{
    public class MqttConsumerClientFactory : IConsumerClientFactory
    {

        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly IOptions<CAPMqttOptions> _mqttOptions;

        public MqttConsumerClientFactory(IOptions<CAPMqttOptions> mqttOptions, IConnectionChannelPool channelPool)
        {
            _mqttOptions = mqttOptions;
            _connectionChannelPool = channelPool;
        }

        public IConsumerClient Create(string groupId)
        {
            try
            {
                var client = new MqttConsumerClient(groupId, _connectionChannelPool, _mqttOptions);
                client.Connect();
                return client;
            }
            catch (Exception e)
            {
                throw new BrokerConnectionException(e);
            }
        }
    }
}
