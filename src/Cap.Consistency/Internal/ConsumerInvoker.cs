using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Cap.Consistency.Internal
{
    public class ConsumerInvoker : IConsumerInvoker
    {
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;
        private readonly ObjectMethodExecutor _executor;
        protected readonly ConsumerContext _consumerContext;

        public ConsumerInvoker(ILogger logger,
            IServiceProvider serviceProvider,
            ConsumerContext consumerContext) {

            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
            _consumerContext = consumerContext ?? throw new ArgumentNullException(nameof(consumerContext));
            _executor = ObjectMethodExecutor.Create(_consumerContext.ConsumerDescriptor.MethodInfo,
                _consumerContext.ConsumerDescriptor.ImplTypeInfo);
        }


        public Task InvokeAsync() {
            try {
                using (_logger.BeginScope("consumer invoker begin")) {

                    _logger.LogDebug("Executing consumer Topic: {0}", _consumerContext.ConsumerDescriptor.Topic);

                    try {

                        var obj = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, _consumerContext.ConsumerDescriptor.ImplTypeInfo.AsType());

                        var bodyString = Encoding.UTF8.GetString(_consumerContext.DeliverMessage.Body);
                        var firstParameter = _executor.MethodParameters[0];
                        object firstParameterObj = null;
                        if (firstParameter != null) {
                            firstParameterObj = JsonConvert.DeserializeObject(bodyString, firstParameter.ParameterType);
                        }
                        _executor.Execute(obj, firstParameterObj);

                        return Task.CompletedTask;
                    }
                    finally {

                        _logger.LogDebug("Executed consumer method .");
                    }
                }
            }
            finally {

            }
        }

    }
}
