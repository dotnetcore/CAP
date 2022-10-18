using DotNetCore.CAP.AzureServiceBus.Producer;
using Shouldly;
using Xunit;

namespace DotNetCore.CAP.AzureServiceBus.Tests.Producer;

public record MessagePublished;

public class AzureServiceBusProducerBuilderTests
{
    [Fact]
    public void Should_HavePropertiesCorrectlySet_When_BuildMethodIsExecuted()
    {
        var producer = new AzureServiceBusProducerBuilder<MessagePublished>()
            .WithTopic("my-destination")
            .WithSessionEnabled(true)
            .Build();

        producer.ShouldNotBeNull();
        producer.TopicPath.ShouldBe("my-destination");
        producer.EnableSessions.ShouldBeTrue();
        producer.MessageTypeName.ShouldBe(nameof(MessagePublished));
    }
}
