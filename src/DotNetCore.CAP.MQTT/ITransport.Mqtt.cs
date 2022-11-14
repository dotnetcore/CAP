using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Publishing;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DotNetCore.CAP.MQTT
{
    public class MqttTransport : ITransport
    {

        private readonly IConnectionChannelPool _connectionPool;
        private readonly ILogger _logger;

        public MqttTransport(ILogger<MqttTransport> logger, IConnectionChannelPool connectionPool)
        {
            _logger = logger;
            _connectionPool = connectionPool;
        }
        public BrokerAddress BrokerAddress => new BrokerAddress("Mqtt", _connectionPool.HostAddress);

        public async Task<OperateResult> SendAsync(TransportMessage message)
        {
            var mqttClient = _connectionPool.Rent();

            try
            {
                var mqttMessage = new MqttMessage();
                mqttMessage.Headers = message.Headers;
                mqttMessage.Body = message.Body;
                var applicationMessageBuild = new MqttApplicationMessageBuilder()
                .WithTopic(message.GetName())      
                .WithPayload(JsonConvert.SerializeObject(mqttMessage))
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);// qos
                var result = await mqttClient.PublishAsync(applicationMessageBuild.Build());

                if (result.ReasonCode == MqttClientPublishReasonCode.Success)
                {
                    _logger.LogDebug($"mqtt topic message [{message.GetName()}] has been published.");

                    return OperateResult.Success;
                }
                throw new PublisherSentFailedException("mqtt message persisted failed!");
            }
            catch (Exception ex)
            {
                var wapperEx = new PublisherSentFailedException(ex.Message, ex);

                return OperateResult.Failed(wapperEx);
            }
            finally
            {
                _connectionPool.Return(mqttClient);
            }
        }
    }
}
