using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetCore.CAP
{
    /// <summary>
    /// Provides programmatic configuration for the CAP kafka project.
    /// </summary>
    public class KafkaOptions
    {
        public KafkaOptions()
        {
            MainConfig = new Dictionary<string, object>();
        }

        /// <summary>
        /// librdkafka configuration parameters (refer to https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md).
        /// <para>
        /// Topic configuration parameters are specified via the "default.topic.config" sub-dictionary config parameter.
        /// </para>
        /// </summary>
        public IDictionary<string, object> MainConfig { get; private set; }

        /// <summary>
        /// The `bootstrap.servers` item config of `MainConfig`.
        /// <para>
        /// Initial list of brokers as a CSV list of broker host or host:port.
        /// </para>
        /// </summary>
        public string Servers { get; set; }

        internal IEnumerable<KeyValuePair<string, object>> AsRdkafkaConfig()
        {
            if (MainConfig.ContainsKey("bootstrap.servers"))
                return MainConfig.AsEnumerable();

            if (string.IsNullOrEmpty(Servers))
            {
                throw new ArgumentNullException(nameof(Servers));
            }
            else
            {
                MainConfig.Add("bootstrap.servers", Servers);
            }
            MainConfig["enable.auto.commit"] = "false";
            return MainConfig.AsEnumerable();
        }
    }
}