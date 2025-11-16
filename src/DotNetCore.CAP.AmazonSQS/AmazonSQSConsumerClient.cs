// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Auth.AccessControlPolicy;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;

namespace DotNetCore.CAP.AmazonSQS;

internal sealed class AmazonSQSConsumerClient : IConsumerClient
{
    private static readonly object ConnectionLock = new();
    private readonly AmazonSQSOptions _amazonSQSOptions;
    private readonly SemaphoreSlim _semaphore;
    private readonly string _groupId;
    private readonly byte _groupConcurrent;
    private string _queueUrl = string.Empty;

    private IAmazonSimpleNotificationService? _snsClient;
    private IAmazonSQS? _sqsClient;

    public AmazonSQSConsumerClient(string groupId, byte groupConcurrent, IOptions<AmazonSQSOptions> options)
    {
        _groupId = groupId;
        _groupConcurrent = groupConcurrent;
        _amazonSQSOptions = options.Value;
        _semaphore = new SemaphoreSlim(groupConcurrent);
    }

    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

    public BrokerAddress BrokerAddress => new("aws_sqs", _queueUrl);

    public async Task<ICollection<string>> FetchTopicsAsync(IEnumerable<string> topicNames)
    {
        if (topicNames == null) throw new ArgumentNullException(nameof(topicNames));

        await ConnectAsync(true, false).ConfigureAwait(false);

        var topicArns = new List<string>();
        foreach (var topic in topicNames)
        {
            var createTopicRequest = new CreateTopicRequest(topic.NormalizeForAws());

            var createTopicResponse = await _snsClient!.CreateTopicAsync(createTopicRequest).ConfigureAwait(false);

            topicArns.Add(createTopicResponse.TopicArn);
        }

        await GenerateSqsAccessPolicyAsync(topicArns).ConfigureAwait(false);

        return topicArns;
    }

    public async Task SubscribeAsync(IEnumerable<string> topics)
    {
        if (topics == null) throw new ArgumentNullException(nameof(topics));

        await ConnectAsync().ConfigureAwait(false);

        await SubscribeToTopics(topics).ConfigureAwait(false);
    }

    public async Task ListeningAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        await ConnectAsync().ConfigureAwait(false);

        var request = new ReceiveMessageRequest(_queueUrl)
        {
            WaitTimeSeconds = 5,
            MaxNumberOfMessages = 1
        };

        while (true)
        {
            var response = await _sqsClient!.ReceiveMessageAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.Messages.Count == 1)
            {
                if (_groupConcurrent > 0)
                {
                    await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                    _ = Task.Run(() => ConsumeAsync(), cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await ConsumeAsync().ConfigureAwait(false);
                }

                Task ConsumeAsync()
                {
                    var messageObj = JsonSerializer.Deserialize<SQSReceivedMessage>(response.Messages[0].Body);

                    var header = messageObj!.MessageAttributes.ToDictionary(x => x.Key, x => x.Value.Value);
                    var body = messageObj.Message;

                    var message = new TransportMessage(header, body != null ? Encoding.UTF8.GetBytes(body) : null);

                    message.Headers[Headers.Group] = _groupId;

                    return OnMessageCallback!(message, response.Messages[0].ReceiptHandle);
                }
            }
            else
            {
                cancellationToken.ThrowIfCancellationRequested();
                cancellationToken.WaitHandle.WaitOne(timeout);
            }
        }
    }

    public async Task CommitAsync(object? sender)
    {
        try
        {
            await _sqsClient!.DeleteMessageAsync(_queueUrl, (string)sender!).ConfigureAwait(false);
            _semaphore.Release();
        }
        catch (ReceiptHandleIsInvalidException ex)
        {
            InvalidIdFormatLog(ex.Message);
        }
    }

    public async Task RejectAsync(object? sender)
    {
        try
        {
            await _sqsClient!.ChangeMessageVisibilityAsync(_queueUrl, (string)sender!, 3).ConfigureAwait(false);
            _semaphore.Release();
        }
        catch (MessageNotInflightException ex)
        {
            MessageNotInflightLog(ex.Message);
        }
    }


    public ValueTask DisposeAsync()
    {
        _sqsClient?.Dispose();
        _snsClient?.Dispose();
        return ValueTask.CompletedTask;
    }

    // Asynchronous version of Connect to avoid blocking threads during queue creation
    private async Task ConnectAsync(bool initSNS = true, bool initSQS = true)
    {
        // Fast path if already initialized for requested resources
        if ((initSNS && _snsClient == null) || (initSQS && _sqsClient == null))
        {
            if (_snsClient == null && initSNS)
            {
                lock (ConnectionLock)
                {
                    if (_snsClient == null)
                    {
                        if (string.IsNullOrWhiteSpace(_amazonSQSOptions.SNSServiceUrl))
                            _snsClient = _amazonSQSOptions.Credentials != null
                                ? new AmazonSimpleNotificationServiceClient(_amazonSQSOptions.Credentials,
                                    _amazonSQSOptions.Region)
                                : new AmazonSimpleNotificationServiceClient(_amazonSQSOptions.Region);
                        else
                            _snsClient = _amazonSQSOptions.Credentials != null
                                ? new AmazonSimpleNotificationServiceClient(_amazonSQSOptions.Credentials,
                                    new AmazonSimpleNotificationServiceConfig { ServiceURL = _amazonSQSOptions.SNSServiceUrl })
                                : new AmazonSimpleNotificationServiceClient(new AmazonSimpleNotificationServiceConfig
                                { ServiceURL = _amazonSQSOptions.SNSServiceUrl });
                    }
                }
            }

            if (_sqsClient == null && initSQS)
            {
                lock (ConnectionLock)
                {
                    if (_sqsClient == null)
                    {
                        if (string.IsNullOrWhiteSpace(_amazonSQSOptions.SQSServiceUrl))
                            _sqsClient = _amazonSQSOptions.Credentials != null
                                ? new AmazonSQSClient(_amazonSQSOptions.Credentials, _amazonSQSOptions.Region)
                                : new AmazonSQSClient(_amazonSQSOptions.Region);
                        else
                            _sqsClient = _amazonSQSOptions.Credentials != null
                                ? new AmazonSQSClient(_amazonSQSOptions.Credentials,
                                    new AmazonSQSConfig { ServiceURL = _amazonSQSOptions.SQSServiceUrl })
                                : new AmazonSQSClient(new AmazonSQSConfig { ServiceURL = _amazonSQSOptions.SQSServiceUrl });
                    }
                }

                if (string.IsNullOrWhiteSpace(_queueUrl))
                {
                    // Create or get existing queue URL asynchronously
                    var queueResponse = await _sqsClient!.CreateQueueAsync(_groupId.NormalizeForAws()).ConfigureAwait(false);
                    _queueUrl = queueResponse.QueueUrl;
                }
            }
        }
    }

    #region private methods

    private void InvalidIdFormatLog(string exceptionMessage)
    {
        var logArgs = new LogMessageEventArgs
        {
            LogType = MqLogType.InvalidIdFormat,
            Reason = exceptionMessage
        };

        OnLogCallback!(logArgs);
    }

    private void MessageNotInflightLog(string exceptionMessage)
    {
        var logArgs = new LogMessageEventArgs
        {
            LogType = MqLogType.MessageNotInflight,
            Reason = exceptionMessage
        };

        OnLogCallback!(logArgs);
    }

    private async Task GenerateSqsAccessPolicyAsync(IEnumerable<string> topicArns)
    {
        await ConnectAsync(false, true).ConfigureAwait(false);

        var queueAttributes = await _sqsClient!.GetAttributesAsync(_queueUrl).ConfigureAwait(false);

        var sqsQueueArn = queueAttributes["QueueArn"];

        var policy = queueAttributes.TryGetValue("Policy", out var policyStr) && !string.IsNullOrEmpty(policyStr)
            ? Policy.FromJson(policyStr)
            : new Policy();

        var topicArnsToAllow = topicArns
            .Where(a => !policy.HasSqsPermission(a, sqsQueueArn))
            .ToList();

        if (!topicArnsToAllow.Any()) return;

        policy.AddSqsPermissions(topicArnsToAllow, sqsQueueArn);
        policy.CompactSqsPermissions(sqsQueueArn);

        var setAttributes = new Dictionary<string, string> { { "Policy", policy.ToJson() } };
        await _sqsClient.SetAttributesAsync(_queueUrl, setAttributes).ConfigureAwait(false);
    }

    private async Task SubscribeToTopics(IEnumerable<string> topics)
    {
        var queueAttributes = await _sqsClient!.GetAttributesAsync(_queueUrl).ConfigureAwait(false);

        var sqsQueueArn = queueAttributes["QueueArn"];
        foreach (var topicArn in topics)
        {
            await _snsClient!.SubscribeAsync(new SubscribeRequest
            {
                TopicArn = topicArn,
                Protocol = "sqs",
                Endpoint = sqsQueueArn
            })
                .ConfigureAwait(false);
        }
    }

    #endregion
}