using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace DotNetCore.CAP.MQTT
{
    public class ConnectionChannelPool : IConnectionChannelPool, IDisposable
    {
        private readonly CAPMqttOptions _options;
        private readonly ConcurrentQueue<IMqttClient> _mqttClientPool;
        private int _pCount;
        private int _maxSize;
        public string HostAddress => $"{_options.Server}:{_options.Port}";
        public ConnectionChannelPool(ILogger<ConnectionChannelPool> logger, IOptions<CAPMqttOptions> options)
        {
            _options = options.Value;
            _mqttClientPool = new ConcurrentQueue<IMqttClient>();
            _maxSize = _options.ConnectionPoolSize;
            logger.LogDebug("CAP Mqtt servers: {0}", $"{_options.Server}:{_options.Port}");
        }
        public IMqttClient Rent()
        {
            if (_mqttClientPool.TryDequeue(out var mqttClient))
            {
                Interlocked.Decrement(ref _pCount);

                return mqttClient;
            }

            MqttClientOptionsBuilder optionsBuilder = new MqttClientOptionsBuilder();
            optionsBuilder.WithTcpServer(_options.Server, _options.Port);

            if (!string.IsNullOrEmpty(_options.UserName) && !string.IsNullOrEmpty(_options.Password))
            {
                optionsBuilder.WithCredentials(_options.UserName, _options.Password);
            }

            optionsBuilder.WithClientId(_options.ClientId);

            optionsBuilder.WithKeepAlivePeriod(TimeSpan.FromSeconds(2000));
            optionsBuilder.WithCleanSession(true);
            optionsBuilder.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V310);
            optionsBuilder.WithCommunicationTimeout(TimeSpan.FromHours(1));
            IMqttClientOptions mqttClientOptions = optionsBuilder.Build();

            mqttClient = BuildMqttClient(mqttClientOptions);
            return mqttClient;
        }

        public bool Return(IMqttClient context)
        {
            if (Interlocked.Increment(ref _pCount) <= _maxSize)
            {
                _mqttClientPool.Enqueue(context);

                return true;
            }

            context.Dispose();

            Interlocked.Decrement(ref _pCount);

            return false;
        }

        public void Dispose()
        {
            _maxSize = 0;

            while (_mqttClientPool.TryDequeue(out var context))
            {
                context.Dispose();

            }
        }

        protected virtual IMqttClient BuildMqttClient(IMqttClientOptions options)
        {
            IMqttClient mqttClient = new MqttFactory().CreateMqttClient();
            mqttClient.ConnectAsync(options).GetAwaiter().GetResult();
            return mqttClient;
        }
    }
}
