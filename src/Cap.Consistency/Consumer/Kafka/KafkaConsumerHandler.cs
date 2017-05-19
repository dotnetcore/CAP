using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Cap.Consistency.Consumer.Kafka
{
    public class KafkaConsumerHandler : IConsumerHandler
    {
        public readonly QMessageFinder _finder;

        public KafkaConsumerHandler(QMessageFinder finder) {
            _finder = finder;
        }

        public void Start(IEnumerable<IConsumerService> consumers) {

            var methods = _finder.GetQMessageMethodInfo(consumers.Select(x => x.GetType()).ToArray());
            

        }

        public void Stop() {
            throw new NotImplementedException();
        }
    }
}
