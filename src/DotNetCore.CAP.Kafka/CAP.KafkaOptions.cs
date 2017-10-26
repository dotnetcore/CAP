using System;
using System.Collections.Concurrent;
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
        /// <summary>
        /// librdkafka configuration parameters (refer to https://github.com/edenhill/librdkafka/blob/master/CONFIGURATION.md).
        /// <para>
        /// Topic configuration parameters are specified via the "default.topic.config" sub-dictionary config parameter.
        /// </para>
        /// </summary>
        public readonly ConcurrentDictionary<string, object> MainConfig;

        private IEnumerable<KeyValuePair<string, object>> _kafkaConfig;


        public KafkaOptions()
        {
            MainConfig = new ConcurrentDictionary<string, object>();
        }

        /// <summary>
        /// Producer connection pool size, default is 10
        /// </summary>
        public int ConnectionPoolSize { get; set; } = 10;

        /// <summary>
        /// The `bootstrap.servers` item config of <see cref="MainConfig" />.
        /// <para>
        /// Initial list of brokers as a CSV list of broker host or host:port.
        /// </para>
        /// </summary>
        public string Servers { get; set; }

        internal IEnumerable<KeyValuePair<string, object>> AsKafkaConfig()
        {
            if (_kafkaConfig == null)
            {
                if (string.IsNullOrWhiteSpace(Servers))
                    throw new ArgumentNullException(nameof(Servers));

                MainConfig["bootstrap.servers"] = Servers;
                MainConfig["queue.buffering.max.ms"] = "10";
                MainConfig["socket.blocking.max.ms"] = "10";
                MainConfig["enable.auto.commit"] = "false";
                MainConfig["log.connection.close"] = "false";
                
                _kafkaConfig = MainConfig.AsEnumerable();
            }
            return _kafkaConfig;
        }
    }
}