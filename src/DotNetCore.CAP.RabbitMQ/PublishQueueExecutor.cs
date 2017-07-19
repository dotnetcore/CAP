using System;
using System.Text;
using System.Threading.Tasks;
using DotNetCore.CAP.Processor.States;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DotNetCore.CAP.RabbitMQ
{
    public class PublishQueueExecutor : BasePublishQueueExecutor
    {
        private readonly ILogger _logger;
        private readonly RabbitMQOptions _rabbitMQOptions;

        public PublishQueueExecutor(IStateChanger stateChanger,
            IOptions<RabbitMQOptions> options,
            ILogger<PublishQueueExecutor> logger)
            : base(stateChanger, logger)
        {
            _logger = logger;
            _rabbitMQOptions = options.Value;
        }

        public override Task<OperateResult> PublishAsync(string keyName, string content)
        {
            var factory = new ConnectionFactory()
            {
                HostName = _rabbitMQOptions.HostName,
                UserName = _rabbitMQOptions.UserName,
                Port = _rabbitMQOptions.Port,
                Password = _rabbitMQOptions.Password,
                VirtualHost = _rabbitMQOptions.VirtualHost,
                RequestedConnectionTimeout = _rabbitMQOptions.RequestedConnectionTimeout,
                SocketReadTimeout = _rabbitMQOptions.SocketReadTimeout,
                SocketWriteTimeout = _rabbitMQOptions.SocketWriteTimeout
            };

            try
            {
                using (var connection = factory.CreateConnection())
                using (var channel = connection.CreateModel())
                {
                    var body = Encoding.UTF8.GetBytes(content);

                    channel.ExchangeDeclare(_rabbitMQOptions.TopicExchangeName, RabbitMQOptions.ExchangeType);
                    channel.BasicPublish(exchange: _rabbitMQOptions.TopicExchangeName,
                                         routingKey: keyName,
                                         basicProperties: null,
                                         body: body);

                    _logger.LogDebug($"rabbitmq topic message [{keyName}] has been published.");
                }
                return Task.FromResult(OperateResult.Success);
            }
            catch (Exception ex)
            {
                _logger.LogError($"rabbitmq topic message [{keyName}] has benn raised an exception of sending. the exception is: {ex.Message}");

                return Task.FromResult(OperateResult.Failed(ex,
                    new OperateError()
                    {
                        Code = ex.HResult.ToString(),
                        Description = ex.Message
                    }));
            }
        }
    }
}