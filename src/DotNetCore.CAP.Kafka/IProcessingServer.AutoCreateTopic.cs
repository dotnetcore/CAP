// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    public class AutoCreateTopic : IProcessingServer
    {
        private readonly ILogger _logger;
        private readonly KafkaOptions _kafkaOptions;
        private readonly MethodMatcherCache _selector;

        public AutoCreateTopic(
            ILogger<AutoCreateTopic> logger,
            IOptions<KafkaOptions> options,
            MethodMatcherCache selector)
        {
            _logger = logger;
            _kafkaOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
            _selector = selector;
        }

        public void Start()
        {
            try
            {
                var config = new AdminClientConfig { BootstrapServers = _kafkaOptions.Servers };

                var topics = _selector.GetAllTopics();

                using var adminClient = new AdminClientBuilder(config).Build();

                adminClient.CreateTopicsAsync(topics.Select(x => new TopicSpecification
                {
                    Name = x
                })).GetAwaiter().GetResult();

                _logger.LogInformation("Topic is automatically created successfully!");
            }
            catch (CreateTopicsException ex) when (ex.Message.Contains("already exists"))
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error was encountered when automatically creating topic!");
            }
            finally
            {
                KafkaConsumerClientFactory.WaitCreateTopic.Set();
            }
        }

        public void Dispose()
        {

        }
    }
}
