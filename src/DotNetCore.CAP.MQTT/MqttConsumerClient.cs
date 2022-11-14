using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace DotNetCore.CAP.MQTT
{
    public class MqttConsumerClient : IConsumerClient
    {
        private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(initialCount: 1, maxCount: 1);
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly CAPMqttOptions _mqttOptions;
        private IMqttClient _mqttClient;
        private string _ClientId;
        public MqttConsumerClient(string ClientId, IConnectionChannelPool connectionChannelPool, IOptions<CAPMqttOptions> mqttOptions)
        {
            _connectionChannelPool = connectionChannelPool;
            _mqttOptions = mqttOptions.Value;
            _ClientId = ClientId;
        }
        public BrokerAddress BrokerAddress => new BrokerAddress("Mqtt", $"{_mqttOptions.Server}:{_mqttOptions.Port}");

        public event EventHandler<TransportMessage> OnMessageReceived;
        public event EventHandler<LogMessageEventArgs> OnLog;


        public void Subscribe(IEnumerable<string> topics)
        {
            if (topics == null)
            {
                throw new ArgumentNullException(nameof(topics));
            }

            Connect();

            foreach (var topic in topics)
            {
                _mqttClient.SubscribeAsync(new MqttClientSubscribeOptionsBuilder().WithTopicFilter(topic).Build(), CancellationToken.None).GetAwaiter().GetResult();
            }
        }
        public void Commit(object sender)
        {
            if (_mqttClient.IsConnected)
            {
                MqttApplicationMessageReceivedEventArgs args = (MqttApplicationMessageReceivedEventArgs)sender;
                args.AcknowledgeAsync(CancellationToken.None).GetAwaiter().GetResult();
            }
        }

        public void Reject(object sender)
        {
            throw new NotImplementedException();

        }
        public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
        {
            Connect();

            _mqttClient.UseApplicationMessageReceivedHandler(MessageReceived);
            _mqttClient.UseDisconnectedHandler(Disconnected);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
        }


        public void Connect()
        {
            if (_mqttClient.IsConnected)
            {
                return;
            }

            _connectionLock.Wait();

            try
            {
                if (_mqttClient == null)
                {
                    _mqttClient = new MqttFactory().CreateMqttClient();
                    MqttClientOptionsBuilder optionsBuilder = new MqttClientOptionsBuilder();
                    // 设置服务器端地址
                    optionsBuilder.WithTcpServer(_mqttOptions.Server, _mqttOptions.Port);

                    // 设置鉴权参数
                    if (!string.IsNullOrEmpty(_mqttOptions.UserName) && !string.IsNullOrEmpty(_mqttOptions.Password))
                    {
                        optionsBuilder.WithCredentials(_mqttOptions.UserName, _mqttOptions.Password);
                    }

                    // 设置客户端序列号
                    optionsBuilder.WithClientId(_ClientId);

                    optionsBuilder.WithKeepAlivePeriod(TimeSpan.FromSeconds(2000));
                    optionsBuilder.WithCleanSession(true);
                    optionsBuilder.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V310);
                    optionsBuilder.WithCommunicationTimeout(TimeSpan.FromHours(1));
                    // 创建选项
                    IMqttClientOptions mqttClientOptions = optionsBuilder.Build();

                    _mqttClient.ConnectAsync(mqttClientOptions).GetAwaiter().GetResult();
                }
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                _connectionLock.Release();
            }
        }

        public void Dispose()
        {
            _mqttClient?.Dispose();
        }


        private void MessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            var payload = args.ApplicationMessage.Payload;
            var mqttmesage = JsonConvert.DeserializeObject<MqttMessage>(Encoding.UTF8.GetString(payload));

            mqttmesage.Headers.Add(Headers.Group, _ClientId);

            var message = new TransportMessage(mqttmesage.Headers, mqttmesage.Body);

            OnMessageReceived?.Invoke(args, message);
        }

        private void Disconnected(MqttClientDisconnectedEventArgs args)
        {

        }



    }
}
