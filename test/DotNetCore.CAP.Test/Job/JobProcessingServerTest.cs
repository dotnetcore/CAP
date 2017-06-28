using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using DotNetCore.CAP.Job;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DotNetCore.CAP.Test.Job
{
    public class JobProcessingServerTest
    {
        private CancellationTokenSource _cancellationTokenSource;
        private ProcessingContext _context;
        private CapOptions _options;
        private IServiceProvider _provider;
        private Mock<ICapMessageStore> _mockStorage;

        public JobProcessingServerTest()
        {
            _options = new CapOptions()
            {
                PollingDelay = 0
            };
            _mockStorage = new Mock<ICapMessageStore>();
            _cancellationTokenSource = new CancellationTokenSource();

            var services = new ServiceCollection();
            services.AddTransient<JobProcessingServer>();
            services.AddTransient<DefaultCronJobRegistry>();
            services.AddLogging();
            services.AddSingleton(_options);
            services.AddSingleton(_mockStorage.Object); 
            _provider = services.BuildServiceProvider();

            _context = new ProcessingContext(_provider, null, _cancellationTokenSource.Token);
        }

        //[Fact]
        //public async Task ProcessAsync_CancellationTokenCancelled_ThrowsImmediately()
        //{
        //    // Arrange
        //    _cancellationTokenSource.Cancel();
        //    var fixture = Create();

        //    // Act
        //    await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.s(_context));
        //}

        //[Fact]
        //public async Task ProcessAsync()
        //{
        //    // Arrange
        //    var job = new CronJob(
        //        InvocationData.Serialize(
        //            MethodInvocation.FromExpression(() => Method())).Serialize());

        //    var mockFetchedJob = Mock.Get(Mock.Of<IFetchedJob>(fj => fj.JobId == 42));

        //    _mockStorageConnection
        //        .Setup(m => m.FetchNextJobAsync())
        //        .ReturnsAsync(mockFetchedJob.Object).Verifiable();

        //    _mockStorageConnection
        //        .Setup(m => m.GetJobAsync(42))
        //        .ReturnsAsync(job).Verifiable();

        //    var fixture = Create();

        //    // Act
        //    fixture.Start();

        //    // Assert
        //    _mockStorageConnection.VerifyAll();
        //    _mockStateChanger.Verify(m => m.ChangeState(job, It.IsAny<SucceededState>(), It.IsAny<IStorageTransaction>()));
        //    mockFetchedJob.Verify(m => m.Requeue(), Times.Never);
        //    mockFetchedJob.Verify(m => m.RemoveFromQueue());
        //}

        //[Fact]
        //public async Task ProcessAsync_Exception()
        //{
        //    // Arrange
        //    var job = new Job(
        //        InvocationData.Serialize(
        //            MethodInvocation.FromExpression(() => Throw())).Serialize());

        //    var mockFetchedJob = Mock.Get(Mock.Of<IFetchedJob>(fj => fj.JobId == 42));

        //    _mockStorageConnection
        //        .Setup(m => m.FetchNextJobAsync())
        //        .ReturnsAsync(mockFetchedJob.Object);

        //    _mockStorageConnection
        //        .Setup(m => m.GetJobAsync(42))
        //        .ReturnsAsync(job);

        //    _mockStateChanger.Setup(m => m.ChangeState(job, It.IsAny<IState>(), It.IsAny<IStorageTransaction>()))
        //        .Throws<Exception>();

        //    var fixture = Create();

        //    // Act
        //    await fixture.ProcessAsync(_context);

        //    // Assert
        //    job.Retries.Should().Be(0);
        //    mockFetchedJob.Verify(m => m.Requeue());
        //}

        //[Fact]
        //public async Task ProcessAsync_JobThrows()
        //{
        //    // Arrange
        //    var job = new Job(
        //        InvocationData.Serialize(
        //            MethodInvocation.FromExpression(() => Throw())).Serialize());

        //    var mockFetchedJob = Mock.Get(Mock.Of<IFetchedJob>(fj => fj.JobId == 42));

        //    _mockStorageConnection
        //        .Setup(m => m.FetchNextJobAsync())
        //        .ReturnsAsync(mockFetchedJob.Object).Verifiable();

        //    _mockStorageConnection
        //        .Setup(m => m.GetJobAsync(42))
        //        .ReturnsAsync(job).Verifiable();

        //    var fixture = Create();

        //    // Act
        //    await fixture.ProcessAsync(_context);

        //    // Assert
        //    job.Retries.Should().Be(1);
        //    _mockStorageTransaction.Verify(m => m.UpdateJob(job));
        //    _mockStorageConnection.VerifyAll();
        //    _mockStateChanger.Verify(m => m.ChangeState(job, It.IsAny<ScheduledState>(), It.IsAny<IStorageTransaction>()));
        //    mockFetchedJob.Verify(m => m.RemoveFromQueue());
        //}

        //[Fact]
        //public async Task ProcessAsync_JobThrows_WithNoRetry()
        //{
        //    // Arrange
        //    var job = new Job(
        //        InvocationData.Serialize(
        //            MethodInvocation.FromExpression<NoRetryJob>(j => j.Throw())).Serialize());

        //    var mockFetchedJob = Mock.Get(Mock.Of<IFetchedJob>(fj => fj.JobId == 42));

        //    _mockStorageConnection
        //        .Setup(m => m.FetchNextJobAsync())
        //        .ReturnsAsync(mockFetchedJob.Object);

        //    _mockStorageConnection
        //        .Setup(m => m.GetJobAsync(42))
        //        .ReturnsAsync(job);

        //    var fixture = Create();

        //    // Act
        //    await fixture.ProcessAsync(_context);

        //    // Assert
        //    _mockStateChanger.Verify(m => m.ChangeState(job, It.IsAny<FailedState>(), It.IsAny<IStorageTransaction>()));
        //}

        private JobProcessingServer Create()
            => _provider.GetService<JobProcessingServer>();

        //public static void Method() { }

        //public static void Throw() { throw new Exception(); }

        //private class NoRetryJob : IRetryable
        //{
        //    public RetryBehavior RetryBehavior => new RetryBehavior(false);
        //    public void Throw() { throw new Exception(); }
        //}
    }
}
