using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Abstractions.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.Internal
{
    public class DefaultConsumerInvoker : IConsumerInvoker
    {
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;
        private readonly IModelBinder _modelBinder;
        private readonly ObjectMethodExecutor _executor;
        protected readonly ConsumerContext _consumerContext;

        public DefaultConsumerInvoker(ILogger logger,
            IServiceProvider serviceProvider,
            IModelBinder modelBinder,
            ConsumerContext consumerContext)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
            _modelBinder = modelBinder;
            _consumerContext = consumerContext ?? throw new ArgumentNullException(nameof(consumerContext));
            _executor = ObjectMethodExecutor.Create(_consumerContext.ConsumerDescriptor.MethodInfo,
                _consumerContext.ConsumerDescriptor.ImplTypeInfo);
        }

        public Task InvokeAsync()
        {
            try
            {
                using (_logger.BeginScope("consumer invoker begin"))
                {
                    _logger.LogDebug("Executing consumer Topic: {0}", _consumerContext.ConsumerDescriptor.Attribute);

                    try
                    {
                        var obj = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, _consumerContext.ConsumerDescriptor.ImplTypeInfo.AsType());

                        var value = _consumerContext.DeliverMessage.Content;

                        if (_executor.MethodParameters.Length > 0)
                        {
                            var firstParameter = _executor.MethodParameters[0];

                            var bindingContext = ModelBindingContext.CreateBindingContext(value,
                                firstParameter.Name, firstParameter.ParameterType);

                            _modelBinder.BindModelAsync(bindingContext);
                            _executor.Execute(obj, bindingContext.Result);
                        }
                        else
                        {
                            _executor.Execute(obj);
                        }
                        return Task.CompletedTask;
                    }
                    finally
                    {
                        _logger.LogDebug("Executed consumer method .");
                    }
                }
            }
            finally
            {
            }
        }
    }
}