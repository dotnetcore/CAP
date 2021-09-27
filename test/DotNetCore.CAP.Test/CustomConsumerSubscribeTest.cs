using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DotNetCore.CAP.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class CustomConsumerSubscribeTest
    {
        private const string TopicNamePrefix = "topic";
        private const string GroupNamePrefix = "group";

        private readonly IServiceProvider _provider;

        public CustomConsumerSubscribeTest()
        {
            var services = new ServiceCollection();
            services.AddSingleton<IConsumerServiceSelector, MyConsumerServiceSelector>();
            services.AddTransient<IMySubscribe, CustomInterfaceTypesClass>();
            services.AddLogging();
            services.AddCap(x =>
            {
                x.TopicNamePrefix = TopicNamePrefix;
                x.GroupNamePrefix = GroupNamePrefix;
            });
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
            var bestCandidates = selector.SelectBestCandidate($"{TopicNamePrefix}.Candidates.Foo", candidates);

            Assert.NotNull(bestCandidates);
            Assert.NotNull(bestCandidates.MethodInfo);
            Assert.StartsWith(GroupNamePrefix, bestCandidates.Attribute.Group);
            Assert.StartsWith(TopicNamePrefix, bestCandidates.TopicName);
            Assert.Equal(typeof(Task), bestCandidates.MethodInfo.ReturnType);
        }
    }

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

                    var parameters = method.GetParameters()
                    .Select(parameter => new ParameterDescriptor
                    {
                        Name = parameter.Name,
                        ParameterType = parameter.ParameterType,
                        IsFromCap = parameter.GetCustomAttributes(typeof(FromCapAttribute)).Any()
                    }).ToList();

                    yield return new ConsumerExecutorDescriptor
                    {
                        Attribute = new CapSubscribeAttribute(attr.Name)
                        {
                            Group = attr.Group
                        },
                        Parameters = parameters,
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

        public void DistracterMethod()
        {
            Console.WriteLine("DistracterMethod() method has been excuted.");
        }
    }
}