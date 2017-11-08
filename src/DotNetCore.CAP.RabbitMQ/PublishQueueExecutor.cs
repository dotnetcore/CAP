using System;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    internal sealed class PublishQueueExecutor : BasePublishQueueExecutor
    {
        private readonly IConnectionChannelPool _connectionChannelPool;
        private readonly ILogger _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;

        public PublishQueueExecutor(ILogger<PublishQueueExecutor> logger, CapOptions options,
            RabbitMQOptions rabbitMQOptions, IConnectionChannelPool connectionChannelPool, IStateChanger stateChanger)
            : base(options, stateChanger, logger)
        {
            _logger = logger;
            _connectionChannelPool = connectionChannelPool;
            _rabbitMQOptions = rabbitMQOptions;
        }

        public override Task<OperateResult> PublishAsync(string keyName, string content)
        {
            var channel = _connectionChannelPool.Rent();
            try
            {
                var body = Encoding.UTF8.GetBytes(content);

                channel.ExchangeDeclare(_rabbitMQOptions.TopicExchangeName, RabbitMQOptions.ExchangeType, true);
                channel.BasicPublish(_rabbitMQOptions.TopicExchangeName,
                    keyName,
                    null,
                    body);

                _logger.LogDebug($"RabbitMQ topic message [{keyName}] has been published.");

                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    $"RabbitMQ topic message [{keyName}] has been raised an exception of sending. the exception is: {ex.Message}");

                return Task.FromResult(OperateResult.Failed(ex,
                    new OperateError
                    {
                        Code = ex.HResult.ToString(),
                        Description = ex.Message
                    }));
            }
            finally
            {
                var returned = _connectionChannelPool.Return(channel);
                if (!returned)
                    channel.Dispose();
            }
        }
    }
}