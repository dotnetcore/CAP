using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Reflection;

namespace Sample.RabbitMQ.SqlServer.DispatcherPerGroup.TypedConsumers
{
    internal static class QueueHandlersExtensions
    {
        private static readonly Type queueHandlerType = typeof(QueueHandler);

        public static IServiceCollection AddQueueHandlers(this IServiceCollection services, params Assembly[] assemblies)
        {
            assemblies ??= new[] { Assembly.GetEntryAssembly() };

            foreach (var type in assemblies.Distinct().SelectMany(x => x.GetTypes().Where(FilterHandlers)))
            {
                services.AddTransient(queueHandlerType, type);
            }

            return services;
        }

        private static bool FilterHandlers(Type t)
        {
            var topic = t.GetCustomAttribute<QueueHandlerTopicAttribute>();

            return queueHandlerType.IsAssignableFrom(t) && topic != null && t.IsClass && !t.IsAbstract;
        }
    }
}