//using System;
//using System.Collections.Generic;
//using System.Text;
//using System.Linq;
//using System.Threading.Tasks;
//using Cap.Consistency.Abstractions;
//using Cap.Consistency.Infrastructure;
//using Cap.Consistency.Routing;
//using Confluent.Kafka;
//using Confluent.Kafka.Serialization;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;

//namespace Cap.Consistency.Consumer.Kafka
//{
//    public class KafkaConsumerHandler<T> : ConsumerHandler<T> where T : ConsistencyMessage, new()
//    {
      
//        protected override void OnMessageReceieved(T message) {
          
//        }

//        public Task RouteAsync(TopicRouteContext context) {

//            if (context == null) {
//                throw new ArgumentNullException(nameof(context));
//            }

//            context.ServiceProvider = _serviceProvider;

//            var matchs = _selector.SelectCandidates(context);

//            var config = new Dictionary<string, object>
//            {
//                { "group.id", "simple-csharp-consumer" },
//                { "bootstrap.servers", _options.BrokerUrlList }
//            };

//            using (var consumer = new Consumer<Null, string>(config, null, new StringDeserializer(Encoding.UTF8))) {

//                var topicList = matchs.Select(item => new TopicPartitionOffset(item.Topic.Name, item.Topic.Partition, new Offset(item.Topic.Offset)));
//                consumer.Assign(topicList);

//                while (true) {
//                    if (consumer.Consume(out Message<Null, string> msg)) {

//                        T consistencyMessage = new T();
//                        var message = new DeliverMessage() {
//                            MessageKey = msg.Topic,
//                            Body = Encoding.UTF8.GetBytes(msg.Value)
//                        };
//                        var routeContext = new TopicRouteContext(message);

//                        var executeDescriptor = _selector.SelectBestCandidate(routeContext, matchs);

//                        if (executeDescriptor == null) {
//                            _logger.LogInformation("can not be fond topic execute");
//                            return Task.CompletedTask;
//                        }

//                        var consumerContext = new ConsumerContext(executeDescriptor, message);
//                        var invoker = _consumerInvokerFactory.CreateInvoker(consumerContext);

//                        _logger.LogInformation("consumer starting");

//                        return invoker.InvokeAsync();

//                    }
//                }
//            }

//        }
//    }
//}
