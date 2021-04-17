using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace MyConsumerSelector
{
    public class MyConsumerServiceSelector : ConsumerServiceSelector
    {
        private readonly CapOptions _capOptions;

        public MyConsumerServiceSelector(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
            _capOptions = serviceProvider.GetService<IOptions<CapOptions>>().Value;
        }

        protected override IEnumerable<ConsumerExecutorDescriptor> FindConsumersFromInterfaceTypes(IServiceProvider provider)
        {
            var executorDescriptorList = new List<ConsumerExecutorDescriptor>();

            using (var scoped = provider.CreateScope())
            {
                var scopedProvider = scoped.ServiceProvider;
                var consumerServices = scopedProvider.GetServices<IMySubscribe>();
                foreach (var service in consumerServices)
                {
                    var typeInfo = service.GetType().GetTypeInfo();
                    if (!typeof(IMySubscribe).GetTypeInfo().IsAssignableFrom(typeInfo))
                    {
                        continue;
                    }

                    executorDescriptorList.AddRange(GetMyDescription(typeInfo));
                }

                return executorDescriptorList;
            }
        }

        private IEnumerable<ConsumerExecutorDescriptor> GetMyDescription(TypeInfo typeInfo)
        {
            foreach (var method in typeInfo.DeclaredMethods)
            {
                var topicAttr = method.GetCustomAttributes<MySubscribeAttribute>(true);
                var topicAttributes = topicAttr as IList<MySubscribeAttribute> ?? topicAttr.ToList();

                if (!topicAttributes.Any())
                {
                    continue;
                }

                foreach (var attr in topicAttributes)
                {
                    if (attr.Group == null)
                    {
                        attr.Group = _capOptions.DefaultGroupName + "." + _capOptions.Version;
                    }
                    else
                    {
                        attr.Group = attr.Group + "." + _capOptions.Version;
                    }

                    if (!string.IsNullOrEmpty(_capOptions.GroupNamePrefix))
                    {
                        attr.Group = $"{_capOptions.GroupNamePrefix}.{attr.Group}";
                    }

                    yield return new ConsumerExecutorDescriptor
                    {
                        Attribute = new CapSubscribeAttribute(attr.Name)
                        {
                            Group = attr.Group
                        },
                        MethodInfo = method,
                        ImplTypeInfo = typeInfo,
                        TopicNamePrefix = _capOptions.TopicNamePrefix
                    };
                }
            }
        }
    }

    public interface IMySubscribe { }

    public class MySubscribeAttribute : Attribute
    {
        public MySubscribeAttribute(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public string Group { get; set; }
    }

    public class CustomInterfaceTypesClass : IMySubscribe
    {
        [MySubscribe("Candidates.Foo")]
        public Task GetFoo()
        {
            Console.WriteLine("GetFoo() method has been excuted.");
            return Task.CompletedTask;
        }

        [MySubscribe("Candidates.Foo2")]
        public void GetFoo2()
        {
            Console.WriteLine("GetFoo2() method has been excuted.");
        }
        
        [MySubscribe("Candidates.Foo3")]
        public void GetFoo3(string message)
        {
            Console.WriteLine($"message {message}");
        }

        public void DistracterMethod()
        {
            Console.WriteLine("DistracterMethod() method has been excuted.");
        }
    }
}