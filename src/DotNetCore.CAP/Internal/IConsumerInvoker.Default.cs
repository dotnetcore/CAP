using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    public class DefaultConsumerInvoker : IConsumerInvoker
    {
        private readonly ILogger _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IModelBinderFactory _modelBinderFactory;
        private readonly ConsumerContext _consumerContext;
        private readonly ObjectMethodExecutor _executor;

        public DefaultConsumerInvoker(ILogger logger,
            IServiceProvider serviceProvider,
            IModelBinderFactory modelBinderFactory,
            ConsumerContext consumerContext)
        {
            _modelBinderFactory = modelBinderFactory;
            _serviceProvider = serviceProvider;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _consumerContext = consumerContext ?? throw new ArgumentNullException(nameof(consumerContext));
            _executor = ObjectMethodExecutor.Create(_consumerContext.ConsumerDescriptor.MethodInfo,
                _consumerContext.ConsumerDescriptor.ImplTypeInfo);
        }

        public async Task InvokeAsync()
        {
            using (_logger.BeginScope("consumer invoker begin"))
            {
                _logger.LogDebug("Executing consumer Topic: {0}", _consumerContext.ConsumerDescriptor.MethodInfo.Name);

                var obj = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider,
                    _consumerContext.ConsumerDescriptor.ImplTypeInfo.AsType());

                var value = _consumerContext.DeliverMessage.Content;

                if (_executor.MethodParameters.Length > 0)
                {
                    var firstParameter = _executor.MethodParameters[0];
                    var binder = _modelBinderFactory.CreateBinder(firstParameter);
                    var result = await binder.BindModelAsync(value);
                    if (result.IsSuccess)
                    {
                        _executor.Execute(obj, result.Model);
                    }
                    else
                    {
                        _logger.LogWarning($"Parameters:{firstParameter.Name} bind failed!");
                    }
                }
                else
                {
                    _executor.Execute(obj);
                }
            }
        }
    }
}