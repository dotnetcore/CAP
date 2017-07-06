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
        protected readonly ILogger Logger;
        protected readonly IServiceProvider ServiceProvider;
        protected readonly ConsumerContext ConsumerContext;

        private readonly IModelBinder _modelBinder;
        private readonly ObjectMethodExecutor _executor;

        public DefaultConsumerInvoker(ILogger logger,
            IServiceProvider serviceProvider,
            IModelBinder modelBinder,
            ConsumerContext consumerContext)
        {
            _modelBinder = modelBinder;
            _executor = ObjectMethodExecutor.Create(ConsumerContext.ConsumerDescriptor.MethodInfo,
                ConsumerContext.ConsumerDescriptor.ImplTypeInfo);

            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            ServiceProvider = serviceProvider;
            ConsumerContext = consumerContext ?? throw new ArgumentNullException(nameof(consumerContext));
        }

        public Task InvokeAsync()
        {
            using (Logger.BeginScope("consumer invoker begin"))
            {
                Logger.LogDebug("Executing consumer Topic: {0}", ConsumerContext.ConsumerDescriptor.MethodInfo.Name);

                var obj = ActivatorUtilities.GetServiceOrCreateInstance(ServiceProvider,
                    ConsumerContext.ConsumerDescriptor.ImplTypeInfo.AsType());

                var value = ConsumerContext.DeliverMessage.Content;

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
        }
    }
}