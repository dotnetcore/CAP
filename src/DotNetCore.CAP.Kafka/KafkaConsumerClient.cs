﻿// Copyright (c) .NET Core Community. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Messages;
using DotNetCore.CAP.Transport;
using Microsoft.Extensions.Options;
using Headers = DotNetCore.CAP.Messages.Headers;

namespace DotNetCore.CAP.Kafka;

public class KafkaConsumerClient : IConsumerClient
{
    private static readonly object Lock = new();
    private readonly string _groupId;
    private readonly byte _groupConcurrent;
    private readonly SemaphoreSlim _semaphore;
    private readonly KafkaOptions _kafkaOptions;
    private readonly IServiceProvider _serviceProvider;
    private IConsumer<string, byte[]>? _consumerClient;

    public KafkaConsumerClient(string groupId, byte groupConcurrent,
        IOptions<KafkaOptions> options, IServiceProvider serviceProvider)
    {
        _groupId = groupId;
        _groupConcurrent = groupConcurrent;
        _semaphore = new SemaphoreSlim(groupConcurrent);
        _serviceProvider = serviceProvider;
        _kafkaOptions = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Func<TransportMessage, object?, Task>? OnMessageCallback { get; set; }

    public Action<LogMessageEventArgs>? OnLogCallback { get; set; }

    public BrokerAddress BrokerAddress => new("Kafka", _kafkaOptions.Servers);

    public ICollection<string> FetchTopics(IEnumerable<string> topicNames)
    {
        if (topicNames == null) throw new ArgumentNullException(nameof(topicNames));

        var regexTopicNames = topicNames.Select(Helper.WildcardToRegex).ToList();

        try
        {
            var config = new AdminClientConfig(_kafkaOptions.MainConfig) { BootstrapServers = _kafkaOptions.Servers };

            using var adminClient = new AdminClientBuilder(config).Build();

            adminClient.CreateTopicsAsync(regexTopicNames.Select(x => new TopicSpecification
            {
                Name = x,
                NumPartitions = _kafkaOptions.TopicOptions.NumPartitions,
                ReplicationFactor = _kafkaOptions.TopicOptions.ReplicationFactor
            })).GetAwaiter().GetResult();
        }
        catch (CreateTopicsException ex) when (ex.Message.Contains("already exists"))
        {
        }
        catch (Exception ex)
        {
            var logArgs = new LogMessageEventArgs
            {
                LogType = MqLogType.ConsumeError,
                Reason = "An error was encountered when automatically creating topic! -->" + ex.Message
            };
            OnLogCallback!(logArgs);
        }

        return regexTopicNames;
    }

    public void Subscribe(IEnumerable<string> topics)
    {
        if (topics == null) throw new ArgumentNullException(nameof(topics));

        Connect();

        _consumerClient!.Subscribe(topics);
    }

    public void Listening(TimeSpan timeout, CancellationToken cancellationToken)
    {
        Connect();

        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<string, byte[]> consumerResult;

            try
            {
                consumerResult = _consumerClient!.Consume(timeout);

                if (consumerResult == null) continue;
                if (consumerResult.IsPartitionEOF || consumerResult.Message.Value == null) continue;
            }
            catch (ConsumeException e) when (_kafkaOptions.RetriableErrorCodes.Contains(e.Error.Code))
            {
                var logArgs = new LogMessageEventArgs
                {
                    LogType = MqLogType.ConsumeRetries,
                    Reason = e.Error.ToString()
                };
                OnLogCallback!(logArgs);

                continue;
            }

            if (_groupConcurrent > 0)
            {
                _semaphore.Wait(cancellationToken);
                Task.Run(() => Consume(consumerResult), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                Consume(consumerResult).GetAwaiter().GetResult();
            }
        }
        // ReSharper disable once FunctionNeverReturns
    }

    public void Commit(object? sender)
    {
        _consumerClient!.Commit((ConsumeResult<string, byte[]>)sender!);
        _semaphore.Release();
    }

    public void Reject(object? sender)
    {
        _consumerClient!.Assign(_consumerClient.Assignment);
        _semaphore.Release();
    }

    public void Dispose()
    {
        _consumerClient?.Dispose();
    }

    public void Connect()
    {
        if (_consumerClient != null) return;

        lock (Lock)
        {
            if (_consumerClient == null)
            {
                var config = new ConsumerConfig(new Dictionary<string, string>(_kafkaOptions.MainConfig));
                config.BootstrapServers ??= _kafkaOptions.Servers;
                config.GroupId ??= _groupId;
                config.AutoOffsetReset ??= AutoOffsetReset.Earliest;
                config.AllowAutoCreateTopics ??= true;
                config.EnableAutoCommit ??= false;
                config.LogConnectionClose ??= false;

                _consumerClient = BuildConsumer(config);
            }
        }
    }

    private async Task Consume(ConsumeResult<string, byte[]> consumerResult)
    {
        var headers = new Dictionary<string, string?>(consumerResult.Message.Headers.Count);
        foreach (var header in consumerResult.Message.Headers)
        {
            var val = header.GetValueBytes();
            headers.Add(header.Key, val != null ? Encoding.UTF8.GetString(val) : null);
        }

        headers.Add(Headers.Group, _groupId);

        if (_kafkaOptions.CustomHeadersBuilder != null)
        {
            var customHeaders = _kafkaOptions.CustomHeadersBuilder(consumerResult, _serviceProvider);
            foreach (var customHeader in customHeaders)
            {
                headers[customHeader.Key] = customHeader.Value;
            }
        }

        var message = new TransportMessage(headers, consumerResult.Message.Value);

        await OnMessageCallback!(message, consumerResult);
    }

    protected virtual IConsumer<string, byte[]> BuildConsumer(ConsumerConfig config)
    {
        return new ConsumerBuilder<string, byte[]>(config)
            .SetErrorHandler(ConsumerClient_OnConsumeError)
            .Build();
    }

    private void ConsumerClient_OnConsumeError(IConsumer<string, byte[]> consumer, Error e)
    {
        var logArgs = new LogMessageEventArgs
        {
            LogType = MqLogType.ServerConnError,
            Reason = $"An error occurred during connect kafka --> {e.Reason}"
        };
        OnLogCallback!(logArgs);
    }
}