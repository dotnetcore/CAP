using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RdKafka;
using System.Text;

namespace Cap.Consistency.Server
{
    public class ConsistencyServer : IConsistencyServer
    {
        private Stack<IDisposable> _disposables;
        private readonly IApplicationLifetime _applicationLifetime;
        private readonly ILogger _logger;
        private readonly IConsumer _consumer;

        public ConsistencyServer(IOptions<ConsistencyServerOptions> options, ILoggerFactory loggerFactory)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            Options = options.Value ?? new ConsistencyServerOptions();
          
            _logger = loggerFactory.CreateLogger(typeof(ConsistencyServer).GetTypeInfo().Namespace);
            _consumer = Options.ApplicationServices.GetService<IConsumer>();
        }

        public ConsistencyServerOptions Options { get; }       
    

        public void Run() {
            //配置消费者组
            var config = new Config() { GroupId = "example-csharp-consumer" };
            using (var consumer = new EventConsumer(config, "127.0.0.1:9092")) {

                //注册一个事件
                consumer.OnMessage += (obj, msg) =>
                {
                    string text = Encoding.UTF8.GetString(msg.Payload, 0, msg.Payload.Length);
                    Console.WriteLine($"Topic: {msg.Topic} Partition: {msg.Partition} Offset: {msg.Offset} {text}");
                };

                //订阅一个或者多个Topic
                consumer.Subscribe(new[] { "testtopic" });

                //启动
                consumer.Start();

                _logger.LogInformation("Started consumer...");
            }
        }
    }
}