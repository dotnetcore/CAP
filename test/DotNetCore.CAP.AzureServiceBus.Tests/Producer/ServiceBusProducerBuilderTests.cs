using DotNetCore.CAP.AzureServiceBus.Producer;
using Shouldly;
using Xunit;

namespace DotNetCore.CAP.AzureServiceBus.Tests.Producer;

public record MessagePublished;

public class ServiceBusProducerBuilderTests
{
    [Fact]
    public void Should_HavePropertiesCorrectlySet_When_BuildMethodIsExecuted()
    {
        var producer = new ServiceBusProducerDescriptorBuilder<MessagePublished>()
            .WithTopic("my-destination")
            .Build();

        producer.ShouldNotBeNull();
        producer.TopicPath.ShouldBe("my-destination");
        producer.MessageTypeName.ShouldBe(nameof(MessagePublished));
    }
}
