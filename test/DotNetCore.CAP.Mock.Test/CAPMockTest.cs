using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using DotNetCore.CAP.Abstractions;
using DotNetCore.CAP.MongoDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Sample.RabbitMQ.MongoDB;
using Xunit;

namespace DotNetCore.CAP.Mock.Test
{
    public class CAPMockTest : IClassFixture<WebApplicationFactory<Startup>>
    {
        private readonly WebApplicationFactory<Startup> _factory;
        public CAPMockTest(WebApplicationFactory<Startup> factory)
        {
            var mock = new Moq.Mock<IMongoTransaction>();
            mock.Setup(t => t.BegeinAsync(It.IsAny<bool>())).ReturnsAsync(new NullMongoTransaction());

            _factory = factory.WithWebHostBuilder(configuration =>
            {
                configuration.ConfigureServices(services =>
                {
                    services.AddMockCap();
                    services.AddSingleton<IMongoTransaction>(mock.Object);//Only for MongoDB
                });
            });
        }

        [Fact]
        public async void MockTest()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/publish");
            Assert.True(response.IsSuccessStatusCode);
        }
    }
}