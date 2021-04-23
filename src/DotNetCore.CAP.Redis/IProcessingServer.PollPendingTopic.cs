// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Linq;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Redis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.Kafka
{
    class PollPendingTopic : IProcessingServer
    {
        private readonly IRedisStreamManager redis;
        private readonly ILogger logger;
        private readonly CapRedisOptions options;
        private readonly MethodMatcherCache selector;

        public PollPendingTopic(
            IRedisStreamManager redis,
            ILogger<PollPendingTopic> logger,
            IOptions<CapRedisOptions> options,
            MethodMatcherCache selector)
        {
            this.redis = redis;
            this.logger = logger;
            this.options = options.Value;
            this.selector = selector;
        }

        public void Start()
        {
            try
            {
                var streams = selector.GetAllTopics();

                foreach (var stream in streams)
                {
                    var streamExist=redis.
                }

                topics.Value.First().TopicName
                using var adminClient = new AdminClientBuilder(config).Build();

                adminClient.CreateTopicsAsync(topics.Select(x => new TopicSpecification
                {
                    Name = x
                })).GetAwaiter().GetResult();

                logger.LogInformation("Topic is automatically created successfully!");
            }
            catch (CreateTopicsException ex) when (ex.Message.Contains("already exists"))
            {
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error was encountered when automatically creating topic!");
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
