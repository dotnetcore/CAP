using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.MQTT
{
    public interface IConnectionChannelPool
    {
        string HostAddress { get; }
        IMqttClient Rent();

        bool Return(IMqttClient context);
    }
}
