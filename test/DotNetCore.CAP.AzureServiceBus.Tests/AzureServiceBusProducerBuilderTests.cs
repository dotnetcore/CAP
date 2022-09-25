using Microsoft.Azure.ServiceBus;
using Shouldly;
using Xunit;

namespace DotNetCore.CAP.AzureServiceBus.Tests;

public record MessagePublished;

public class AzureServiceBusProducerBuilderTests
{
    [Fact]
    public void Should_HavePropertiesCorrectlySet_When_BuildMethodIsExecuted()
    {
        var producer = new AzureServiceBusProducerBuilder<MessagePublished>()
            .To("my-destination")
            .WithConnectionString("test")
            .WithSessionEnabled(true)
            .WithRetryPolicy(new NoRetry())
            .WithCustomHeader("test-header", "test-header-value")
            .Build();

        producer.ShouldNotBeNull();
        producer.TopicPath.ShouldBe("my-destination");
        producer.EnableSessions.ShouldBeTrue();
        producer.RetryPolicy.ShouldBeOfType<NoRetry>();
        producer.CustomHeaders.ShouldNotBeNull();
        producer.CustomHeaders.ShouldNotBeEmpty();
        producer.CustomHeaders.ShouldContainKeyAndValue("test-header","test-header-value");
        producer.MessageTypeFullName.ShouldBe(typeof(MessagePublished).FullName);
        producer.MessageType.ShouldBe(typeof(MessagePublished));
    }
}
