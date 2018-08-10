using System;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Models;
using DotNetCore.CAP.Processor.States;
using Moq;
using Xunit;

namespace DotNetCore.CAP.Test
{
    public class StateChangerTest
    {
        [Fact]
        public void ChangeState()
        {
            // Arrange
            var fixture = Create();
            var message = new CapPublishedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                StatusName = StatusName.Scheduled
            };
            var state = Mock.Of<IState>(s => s.Name == "s" && s.ExpiresAfter == null);
            var mockTransaction = new Mock<IStorageTransaction>();

            // Act
            fixture.ChangeState(message, state, mockTransaction.Object);

            // Assert
            Assert.Equal("s", message.StatusName);
            Assert.Null(message.ExpiresAt);
            Mock.Get(state).Verify(s => s.Apply(message, mockTransaction.Object), Times.Once);
            mockTransaction.Verify(t => t.UpdateMessage(message), Times.Once);
            mockTransaction.Verify(t => t.CommitAsync(), Times.Never);
        }

        [Fact]
        public void ChangeState_ExpiresAfter()
        {
            // Arrange
            var fixture = Create();
            var message = new CapPublishedMessage
            {
                Id = SnowflakeId.Default().NextId(),
                StatusName = StatusName.Scheduled
            };
            var state = Mock.Of<IState>(s => s.Name == "s" && s.ExpiresAfter == TimeSpan.FromHours(1));
            var mockTransaction = new Mock<IStorageTransaction>();

            // Act
            fixture.ChangeState(message, state, mockTransaction.Object);

            // Assert
            Assert.Equal("s", message.StatusName);
            Assert.NotNull(message.ExpiresAt);
            mockTransaction.Verify(t => t.UpdateMessage(message), Times.Once);
            mockTransaction.Verify(t => t.CommitAsync(), Times.Never);
        }

        private StateChanger Create() => new StateChanger();
    }
}