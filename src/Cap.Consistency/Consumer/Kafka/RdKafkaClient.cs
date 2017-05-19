using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cap.Consistency.Route;
using Confluent.Kafka;
using Confluent.Kafka.Serialization;

namespace Cap.Consistency.Consumer.Kafka
{
    public class RdKafkaClient
    {

        private Consumer<string, string> _client;

        public RdKafkaClient() {

        }


        public void Start(TopicRouteContext routeContext )  {

            string brokerList = null;// args[0];
            var topics = new List<string>();// args.Skip(1).ToList();

            var config = new Dictionary<string, object>
            {
                { "group.id", "simple-csharp-consumer" },
                { "bootstrap.servers", brokerList }
            };

            using (var consumer = new Consumer<Null, string>(config, null, new StringDeserializer(Encoding.UTF8))) {
                //consumer.Assign(new List<TopicInfo> { new TopicInfo(topics.First(), 0, 0) });

                while (true) {
                    Message<Null, string> msg;
                    if (consumer.Consume(out msg)) {
                        Console.WriteLine($"Topic: {msg.Topic} Partition: {msg.Partition} Offset: {msg.Offset} {msg.Value}");
                    }
                }
            }

        }
    }
}
