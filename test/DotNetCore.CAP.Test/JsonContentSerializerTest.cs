using System;
using DotNetCore.CAP.Internal;
using DotNetCore.CAP.Models;
using Newtonsoft.Json;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class JsonContentSerializerTest
    {
        [Fact]
        public void CanSerialize()
        {
            //Arrange
            var fixtrue = Create();

            var message = new CapMessageDto
            {
                Id = "1",
                Content = "Content",
                CallbackName = "Callback",
                Timestamp = DateTime.Now
            };

            //Act
            var ret = fixtrue.Serialize(message);

            //Assert
            var expected = JsonConvert.SerializeObject(message);
            Assert.NotNull(ret);
            Assert.Equal(expected, ret);
        }

        [Fact]
        public void CanDeSerialize()
        {
            //Arrange
            var fixtrue = Create();

            var message = new CapMessageDto
            {
                Id = "1",
                Content = "Content",
                CallbackName = "Callback",
                Timestamp = DateTime.Now
            };
            var messageJson = JsonConvert.SerializeObject(message);

            //Act
            var ret = fixtrue.DeSerialize<CapMessageDto>(messageJson);

            //Assert
            Assert.NotNull(ret);
            Assert.Equal(message.Id, ret.Id);
            Assert.Equal(message.Content, ret.Content);
            Assert.Equal(message.CallbackName, ret.CallbackName);
            Assert.Equal(message.Timestamp, ret.Timestamp);
        }

        private JsonContentSerializer Create() => new JsonContentSerializer();
    }
}
