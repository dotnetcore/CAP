using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class CustomConsumerSubscribeTest
    {
        private readonly IServiceProvider _provider;

        public CustomConsumerSubscribeTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConsumerServiceSelector, MyConsumerServiceSelector>();
            services.AddTransient<IMySubscribe, CustomInterfaceTypesClass>();
            services.AddLogging();
            services.AddCap(x => { });
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public void CanFindAllConsumerService()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();

            Assert.Equal(2, candidates.Count);
        }

        [Fact]
        public void CanFindSpecifiedTopic()
        {
            var selector = _provider.GetRequiredService<IConsumerServiceSelector>();
            var candidates = selector.SelectCandidates();
            var bestCandidates = selector.SelectBestCandidate("Candidates.Foo", candidates);

            Assert.NotNull(bestCandidates);
            Assert.NotNull(bestCandidates.MethodInfo);
            Assert.Equal(typeof(Task), bestCandidates.MethodInfo.ReturnType);
        }
    }

    public class MyConsumerServiceSelector : DefaultConsumerServiceSelector
    {
        private readonly CapOptions _capOptions;

        public MyConsumerServiceSelector(IServiceProvider serviceProvider, CapOptions capOptions)
            : base(serviceProvider, capOptions)
        {
            _capOptions = capOptions;
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
                        attr.Group = _capOptions.DefaultGroup + "." + _capOptions.Version;
                    }
                    else
                    {
                        attr.Group = attr.Group + "." + _capOptions.Version;
                    }

                    yield return new ConsumerExecutorDescriptor
                    {
                        Attribute = new CapSubscribeAttribute(attr.Name)
                        {
                            Group = attr.Group
                        },
                        MethodInfo = method,
                        ImplTypeInfo = typeInfo
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

        public void DistracterMethod()
        {
            Console.WriteLine("DistracterMethod() method has been excuted.");
        }
    }
}