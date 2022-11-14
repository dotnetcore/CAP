using DotNetCore.CAP;
using System;

namespace DotNetCore.CAP.MQTT
{
    public static class CapOptionsExtensions
    {
        public static CapOptions UseMQTT(this CapOptions options, string server)
        {
            return options.UseMQTT(option => { option.Server = server; });
        }

        public static CapOptions UseMQTT(this CapOptions options, Action<CAPMqttOptions> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }
            options.RegisterExtension(new MqttCAPOptionsExtension(configure));
            return options;
        }
    }
}
