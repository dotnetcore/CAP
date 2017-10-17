using System;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class CallbackMessageSenderTest
    {
        private IServiceProvider _provider;
        private Mock<ICallbackPublisher> _mockCallbackPublisher;
        private Mock<IContentSerializer> _mockContentSerializer;
        private Mock<IMessagePacker> _mockMessagePack;

        public CallbackMessageSenderTest()
        {
            _mockCallbackPublisher = new Mock<ICallbackPublisher>();
            _mockContentSerializer = new Mock<IContentSerializer>();
            _mockMessagePack = new Mock<IMessagePacker>();

            var services = new ServiceCollection();
            services.AddTransient<CallbackMessageSender>();
            services.AddLogging();
            services.AddSingleton(_mockCallbackPublisher.Object);
            services.AddSingleton(_mockContentSerializer.Object);
            services.AddSingleton(_mockMessagePack.Object);
            _provider = services.BuildServiceProvider();
        }

        [Fact]
        public async void SendAsync_CanSend()
        {
            // Arrange
            _mockCallbackPublisher
                .Setup(x => x.PublishAsync(It.IsAny<CapPublishedMessage>()))
                .Returns(Task.CompletedTask).Verifiable();

            _mockContentSerializer
                .Setup(x => x.Serialize(It.IsAny<object>()))
                .Returns("").Verifiable();

            _mockMessagePack
                .Setup(x => x.Pack(It.IsAny<CapMessage>()))
                .Returns("").Verifiable();

            var fixture = Create();

            // Act
            await fixture.SendAsync(null, null, Mock.Of<object>());

            // Assert
            _mockCallbackPublisher.VerifyAll();
            _mockContentSerializer.Verify();
            _mockMessagePack.Verify();
        }

        private CallbackMessageSender Create()
            => _provider.GetService<CallbackMessageSender>();
    }
}

namespace Samples
{

    public interface IFoo
    {
        int Age { get; set; }
        string Name { get; set; }
    }

    public class FooTest
    {
        [Fact]
        public void CanSetProperty()
        {
            var mockFoo = new Mock<IFoo>();
            mockFoo.Setup(x => x.Name).Returns("NameProerties");

            Assert.Equal("NameProerties", mockFoo.Object.Name);
        }
    }

}
