using System;
using System.Reflection;
using DotNetCore.CAP.Diagnostics;
using DotNetCore.CAP.Infrastructure;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class HelperTest
    {

        [Fact]
        public void ToTimestampTest()
        {
            //Arrange
            var time = DateTime.Parse("2018-01-01 00:00:00");

            //Act
            var result = Helper.ToTimestamp(time);

            //Assert
            Assert.Equal(1514764800, result);
        }

        [Fact]
        public void FromTimestampTest()
        {
            //Arrange
            var time = DateTime.Parse("2018-01-01 00:00:00");

            //Act
            var result = Helper.FromTimestamp(1514764800);

            //Assert
            Assert.Equal(time, result);
        }

        [Fact]
        public void IsControllerTest()
        {
            //Arrange
            var typeInfo = typeof(HomeController).GetTypeInfo();

            //Act
            var result = Helper.IsController(typeInfo);

            //Assert
            Assert.True(result);
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(decimal))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(Uri))]
        public void IsSimpleTypeTest(Type type)
        {
            //Act
            var result = Helper.IsComplexType(type);

            //Assert 
            Assert.False(result);
        }

        [Theory]
        [InlineData(typeof(HomeController))]
        [InlineData(typeof(Exception))]
        [InlineData(typeof(Person))]
        public void IsComplexTypeTest(Type type)
        {
            //Act
            var result = Helper.IsComplexType(type);

            //Assert 
            Assert.True(result);
        }

        [Fact]
        public void AddExceptionPropertyTest()
        {
            //Arrange
            var json = "{}";
            var exception = new Exception("Test Exception Message")
            {
                Source = "Test Source",
                InnerException = { }
            };

            var expected = new
            {
                ExceptionMessage = new
                {
                    Source = "Test Source",
                    Message = "Test Exception Message",
                    InnerMessage = new { }
                }
            };

            //Act
            var result = Helper.AddExceptionProperty(json, exception);

            //Assert
            var jObj = JObject.Parse(result);
            Assert.Equal(jObj["ExceptionMessage"]["Source"].Value<string>(), expected.ExceptionMessage.Source);
            Assert.Equal(jObj["ExceptionMessage"]["Message"].Value<string>(), expected.ExceptionMessage.Message);
        }

        [Theory]
        [InlineData("10.0.0.1")]
        [InlineData("172.16.0.1")]
        [InlineData("192.168.1.1")]
        public void IsInnerIPTest(string ipAddress)
        {
            Assert.True(Helper.IsInnerIP(ipAddress));
        }

        [Fact]
        public void AddTracingHeaderPropertyTest()
        {
            //Arrange
            var json = "{}";
            var header = new TracingHeaders { { "key", "value" } };

            //Act
            var result = Helper.AddTracingHeaderProperty(json, header);

            //Assert
            var expected = "{\"TracingHeaders\":{\"key\":\"value\"}}";
            Assert.Equal(expected, result);
        }

        [Fact]
        public void TryExtractTracingHeadersTest()
        {
            //Arrange
            var json = "{\"TracingHeaders\":{\"key\":\"value\"}}";
            TracingHeaders header = null;
            string removedHeadersJson = "";

            //Act
            var result = Helper.TryExtractTracingHeaders(json, out header, out removedHeadersJson);

            //Assert
            Assert.True(result);
            Assert.NotNull(header);
            Assert.Single(header);
            Assert.Equal("{}", removedHeadersJson);
        }
    }
}
