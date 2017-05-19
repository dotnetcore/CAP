using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Cap.Consistency.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Cap.Consistency.Internal
{
    public class ConsumerInvoker : IConsumerInvoker
    {
        protected readonly ILogger _logger;
        protected readonly IServiceProvider _serviceProvider;
        private readonly ObjectMethodExecutor _executor;
        protected readonly ConsumerContext _consumerContext;

        private Dictionary<string, object> _arguments;

        public ConsumerInvoker(ILogger logger,
            IServiceProvider serviceProvider,
            ConsumerContext consumerContext,
            ObjectMethodExecutor objectMethodExecutor) {
            if (logger == null) {
                throw new ArgumentNullException(nameof(logger));
            }

            if (consumerContext == null) {
                throw new ArgumentNullException(nameof(consumerContext));
            }

            if (objectMethodExecutor == null) {
                throw new ArgumentNullException(nameof(objectMethodExecutor));
            }

            _logger = logger;
            _serviceProvider = serviceProvider;
            _consumerContext = consumerContext;
            _executor = ObjectMethodExecutor.Create(_consumerContext.ConsumerDescriptor.MethodInfo,
                _consumerContext.ConsumerDescriptor.ImplType.GetTypeInfo());
        }


        public Task InvokeAsync() {
            try {
                using (_logger.BeginScope("consumer invoker begin")) {

                    _logger.LogDebug("Executing consumer Topic: {0}", _consumerContext.ConsumerDescriptor.Topic);

                    try {

                        var obj = ActivatorUtilities.GetServiceOrCreateInstance(_serviceProvider, _consumerContext.ConsumerDescriptor.ImplType);
                        _executor.Execute(obj, null);
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

        private object _controller;

        private async Task InvokeConsumerMethodAsync() {
            var controllerContext = _consumerContext;
            var executor = _executor;
            var controller = _controller;
            var arguments = _arguments;
            var orderedArguments = ConsumerMethodExecutor.PrepareArguments(arguments, executor);

            var logger = _logger;

            object result = null;
            try {

                var returnType = executor.MethodReturnType;
                if (returnType == typeof(void)) {
                    executor.Execute(controller, orderedArguments);
                    result = new object();
                }
                else if (returnType == typeof(Task)) {
                    await (Task)executor.Execute(controller, orderedArguments);
                    result = new object();
                }
                //else if (executor.TaskGenericType == typeof(IActionResult)) {
                //    result = await (Task<IActionResult>)executor.Execute(controller, orderedArguments);
                //    if (result == null) {
                //        throw new InvalidOperationException(
                //            Resources.FormatActionResult_ActionReturnValueCannotBeNull(typeof(IActionResult)));
                //    }
                //}
                //else if (executor.IsTypeAssignableFromIActionResult) {
                //    if (_executor.IsMethodAsync) {
                //        result = (IActionResult)await _executor.ExecuteAsync(controller, orderedArguments);
                //    }
                //    else {
                //        result = (IActionResult)_executor.Execute(controller, orderedArguments);
                //    }

                //    if (result == null) {
                //        throw new InvalidOperationException(
                //            Resources.FormatActionResult_ActionReturnValueCannotBeNull(_executor.TaskGenericType ?? returnType));
                //    }
                //}
                //else if (!executor.IsMethodAsync) {
                //    var resultAsObject = executor.Execute(controller, orderedArguments);
                //    result = resultAsObject as IActionResult ?? new ObjectResult(resultAsObject) {
                //        DeclaredType = returnType,
                //    };
                //}
                //else if (executor.TaskGenericType != null) {
                //    var resultAsObject = await executor.ExecuteAsync(controller, orderedArguments);
                //    result = resultAsObject as IActionResult ?? new ObjectResult(resultAsObject) {
                //        DeclaredType = executor.TaskGenericType,
                //    };
                //}
                //else {
                //    // This will be the case for types which have derived from Task and Task<T> or non Task types.
                //    throw new InvalidOperationException(Resources.FormatActionExecutor_UnexpectedTaskInstance(
                //        executor.MethodInfo.Name,
                //        executor.MethodInfo.DeclaringType));
                //}

                //_result = result;
                // logger.ActionMethodExecuted(controllerContext, result);
            }
            finally {

            }
        }

    }
}
