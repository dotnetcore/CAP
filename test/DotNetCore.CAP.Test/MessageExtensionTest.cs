using System;
using System.Collections.Generic;
using DotNetCore.CAP.Messages;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class MessageExtensionTest
    {
        [Fact]
        public void GetIdTest()
        {
            var msgId = Guid.NewGuid().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageId] = msgId
            };
            var message = new Message(header, null);
            
            Assert.NotNull(message.GetId());
            Assert.Equal(msgId,message.GetId());
        }

        [Fact]
        public void GetNameTest()
        {
            var msgName = Guid.NewGuid().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.MessageName] = msgName
            };
            var message = new Message(header, null);

            Assert.NotNull(message.GetName());
            Assert.Equal(msgName, message.GetName());
        }

        [Fact]
        public void GetCallbackNameTest()
        {
            var callbackName = Guid.NewGuid().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.CallbackName] = callbackName
            };
            var message = new Message(header, null);

            Assert.NotNull(message.GetCallbackName());
            Assert.Equal(callbackName, message.GetCallbackName());
        }

        [Fact]
        public void GetGroupTest()
        {
            var group = Guid.NewGuid().ToString();
            var header = new Dictionary<string, string>()
            {
                [Headers.Group] = group
            };
            var message = new Message(header, null);

            Assert.NotNull(message.GetGroup());
            Assert.Equal(group, message.GetGroup());
        }

        [Fact]
        public void GetCorrelationSequenceTest()
        {
            var seq = 1;
            var header = new Dictionary<string, string>()
            {
                [Headers.CorrelationSequence] = seq.ToString()
            };
            var message = new Message(header, null);

            Assert.Equal(seq, message.GetCorrelationSequence());
        }

        [Fact]
        public void HasExceptionTest()
        {
            var exception = "exception message";
            var header = new Dictionary<string, string>()
            {
                [Headers.Exception] = exception
            };
            var message = new Message(header, null);

            Assert.True(message.HasException());
        }
    }
}
