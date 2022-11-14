using DotNetCore.CAP;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.MQTT
{
    internal sealed class MqttCAPOptionsExtension : ICapOptionsExtension
    {
        private readonly Action<CAPMqttOptions> _configure;

        public MqttCAPOptionsExtension(Action<CAPMqttOptions> configure)
        {
            _configure = configure;
        }
        public void AddServices(IServiceCollection services)
        {
            services.AddSingleton<CapMessageQueueMakerService>();

            services.Configure(_configure);
            services.AddSingleton<ITransport, MqttTransport>();
            services.AddSingleton<IConsumerClientFactory, MqttConsumerClientFactory>();
            services.AddSingleton<IConnectionChannelPool, ConnectionChannelPool>();
        }
    }
}
