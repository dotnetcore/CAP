using System;
using System.Collections.Generic;
using System.Linq;

// ReSharper disable once CheckNamespace
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
        public readonly IDictionary<string, object> MainConfig;

        /// <summary>
        /// The `bootstrap.servers` item config of <see cref="MainConfig"/>.
        /// <para>
        /// Initial list of brokers as a CSV list of broker host or host:port.
        /// </para>
        /// </summary>
        public string Servers { get; set; }

        internal IEnumerable<KeyValuePair<string, object>> AskafkaConfig()
        {
            if (MainConfig.ContainsKey("bootstrap.servers"))
            {
                return MainConfig.AsEnumerable();
            }

            if (string.IsNullOrWhiteSpace(Servers))
            {
                throw new ArgumentNullException(nameof(Servers));
            }

            MainConfig.Add("bootstrap.servers", Servers);

            MainConfig["queue.buffering.max.ms"] = "10";
            MainConfig["socket.blocking.max.ms"] = "10";
            MainConfig["enable.auto.commit"] = "false";

            return MainConfig.AsEnumerable();
        }
    }
}