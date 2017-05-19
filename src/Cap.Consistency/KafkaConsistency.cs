using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Cap.Consistency.Consumer;
using Microsoft.Extensions.DependencyInjection;

namespace Cap.Consistency
{
    public class KafkaConsistency
    {
        private IServiceProvider _serviceProvider;
        private IEnumerable<IConsumerHandler> _handlers;

        public KafkaConsistency(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider;
        }

        public void Start() {
            _handlers = _serviceProvider.GetServices<IConsumerHandler>();
            var services = _serviceProvider.GetServices<IConsumerService>();
            foreach (var handler in _handlers) {
                handler.Start(services);
            }
        }

        public void Stop() {
            foreach (var handler in _handlers) {
                handler.Stop();
            }
        }
    }
}
