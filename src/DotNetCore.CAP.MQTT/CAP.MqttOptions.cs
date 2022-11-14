using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.MQTT
{
    public class CAPMqttOptions
    {
        public string Server { get; set; } = "localhost";
        public int Port { get; set; }
        public string ClientId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public int ConnectionPoolSize { get; set; }
    }
}
