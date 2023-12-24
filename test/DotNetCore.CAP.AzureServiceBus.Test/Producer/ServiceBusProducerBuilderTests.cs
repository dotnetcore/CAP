using DotNetCore.CAP.AzureServiceBus.Producer;
using Shouldly;
using Xunit;

namespace DotNetCore.CAP.AzureServiceBus.Test.Producer;

public record MessagePublished;

public class ServiceBusProducerBuilderTests
{
    [Fact]
    public void Should_HavePropertiesCorrectlySet_When_Obsolete_BuildMethodIsExecuted()
    {
        var producer = new ServiceBusProducerDescriptorBuilder<MessagePublished>()
            .UseTopic("my-destination")
            .Build();

        producer.ShouldNotBeNull();
        producer.TopicPath.ShouldBe("my-destination");
        producer.MessageTypeName.ShouldBe(nameof(MessagePublished));
    }

    [Theory]
    [InlineData("my-destination1", true)]
    [InlineData("my-destination2", false)]
    public void Should_HavePropertiesCorrectlySet_When_BuildMethodIsExecuted(string topicName, bool subscriptionEnabled)
    {
        var builder = new ServiceBusProducerDescriptorBuilder<MessagePublished>()
            .UseTopic(topicName);

        if (subscriptionEnabled)
        {
            builder.WithSubscription();
        }

        var producer = builder.Build();
        producer.ShouldNotBeNull();
        producer.TopicPath.ShouldBe(topicName);
        producer.CreateSubscription.ShouldBe(subscriptionEnabled);
        producer.MessageTypeName.ShouldBe(nameof(MessagePublished));
    }
}
