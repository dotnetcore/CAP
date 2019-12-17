using System;
using System.Reflection;
using DotNetCore.CAP.Internal;
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
        public void IsComplexTypeTest(Type type)
        {
            //Act
            var result = Helper.IsComplexType(type);

            //Assert 
            Assert.True(result);
        }

        [Theory]
        [InlineData("10.0.0.1")]
        [InlineData("172.16.0.1")]
        [InlineData("192.168.1.1")]
        public void IsInnerIPTest(string ipAddress)
        {
            Assert.True(Helper.IsInnerIP(ipAddress));
        }
    }

    public class HomeController
    {
        
    }
}
