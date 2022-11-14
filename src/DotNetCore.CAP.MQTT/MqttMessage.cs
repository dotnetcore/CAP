using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetCore.CAP.MQTT
{
    public class MqttMessage
    {
        public IDictionary<string, string?> Headers { get; set; }
        public byte[]? Body { get; set; }
    }
}
