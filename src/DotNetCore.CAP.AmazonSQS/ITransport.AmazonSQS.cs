// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AmazonSQS
{
    internal sealed class AmazonSQSTransport : ITransport
    {
        private readonly ILogger _logger;
        private readonly IOptions<AmazonSQSOptions> _sqsOptions;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private IAmazonSimpleNotificationService _snsClient;
        private IDictionary<string, string> _topicArnMaps;

        public AmazonSQSTransport(ILogger<AmazonSQSTransport> logger, IOptions<AmazonSQSOptions> sqsOptions)
        {
            _logger = logger;
            _sqsOptions = sqsOptions;
        }

        public BrokerAddress BrokerAddress => new BrokerAddress("RabbitMQ", string.Empty);

        public async Task<OperateResult> SendAsync(TransportMessage message)
        {
            try
            {
                await TryAddTopicArns();

                if (_topicArnMaps.TryGetValue(message.GetName().NormalizeForAws(), out var arn))
                {
                    string bodyJson = null;
                    if (message.Body != null)
                    {
                        bodyJson = Encoding.UTF8.GetString(message.Body);
                    }

                    var attributes = message.Headers.Where(x => x.Value != null).ToDictionary(x => x.Key,
                        x => new MessageAttributeValue
                        {
                            StringValue = x.Value,
                            DataType = "String"
                        });
                    
                    var request = new PublishRequest(arn, bodyJson)
                    {
                        MessageAttributes = attributes
                    };

                    await _snsClient.PublishAsync(request);

                    _logger.LogDebug($"SNS topic message [{message.GetName().NormalizeForAws()}] has been published.");
                    return OperateResult.Success;
                }

                _logger.LogWarning($"Can't be found SNS topics for [{message.GetName().NormalizeForAws()}]");
                return OperateResult.Failed(new OperateError
                {
                    Code = "SNS",
                    Description = $"Can't be found SNS topics for [{message.GetName().NormalizeForAws()}]"
                });
            }
            catch (Exception ex)
            {
                var wrapperEx = new PublisherSentFailedException(ex.Message, ex);
                var errors = new OperateError
                {
                    Code = ex.HResult.ToString(),
                    Description = ex.Message
                };

                return OperateResult.Failed(wrapperEx, errors);
            }
        }

        public async Task<bool> TryAddTopicArns()
        {
            if (_topicArnMaps != null)
            {
                return true;
            }

            await _semaphore.WaitAsync();

            try
            {
                _snsClient = _sqsOptions.Value.Credentials != null
                    ? new AmazonSimpleNotificationServiceClient(_sqsOptions.Value.Credentials, _sqsOptions.Value.Region)
                    : new AmazonSimpleNotificationServiceClient(_sqsOptions.Value.Region);

                if (_topicArnMaps == null)
                {
                    _topicArnMaps = new Dictionary<string, string>();
                    
                    string nextToken = null;
                    do
                    {
                        var topics = nextToken == null
                            ? await _snsClient.ListTopicsAsync()
                            : await _snsClient.ListTopicsAsync(nextToken);
                        topics.Topics.ForEach(x =>
                        {
                            var name = x.TopicArn.Split(':').Last();
                            _topicArnMaps.Add(name, x.TopicArn);
                        });
                        nextToken = topics.NextToken;
                    }
                    while (!string.IsNullOrEmpty(nextToken));

                    return true;
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Init topics from aws sns error!");
            }
            finally
            {
                _semaphore.Release();
            }

            return false;
        }
    }
}