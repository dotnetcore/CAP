//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using DotNetCore.CAP.Models;
//using DotNetCore.CAP.Processor;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Options;
//using Moq;
//using Xunit;

//namespace DotNetCore.CAP.Test
//{
//    public class DefaultDispatcherTest
//    {
//        private CancellationTokenSource _cancellationTokenSource;
//        private ProcessingContext _context;
//        private IServiceProvider _provider;
//        private Mock<IStorageConnection> _mockStorageConnection;

//        public DefaultDispatcherTest()
//        {
//            _mockStorageConnection = new Mock<IStorageConnection>();
           
//            _cancellationTokenSource = new CancellationTokenSource();

//            var services = new ServiceCollection();
//            services.AddLogging();
//            services.Configure<IOptions<CapOptions>>(x => { });
//            services.AddOptions();
//            services.AddSingleton(_mockStorageConnection.Object);
//            _provider = services.BuildServiceProvider();

//            _context = new ProcessingContext(_provider, _cancellationTokenSource.Token);
//        }

//        [Fact]
//        public void MockTest()
//        {
//            Assert.NotNull(_provider.GetServices<IStorageConnection>());
//        }

//        [Fact]
//        public async void ProcessAsync_CancellationTokenCancelled_ThrowsImmediately()
//        {
//            // Arrange
//            _cancellationTokenSource.Cancel();
//            var fixture = Create();

//            // Act
//            await Assert.ThrowsAsync<OperationCanceledException>(() => fixture.ProcessAsync(_context));
//        }

//        [Fact]
//        public async Task ProcessAsync()
//        {
//            // Arrange
//            var job = new CapPublishedMessage
//            {
//            };

//            var mockFetchedJob = Mock.Get(Mock.Of<IFetchedMessage>(fj => fj.MessageId == 42 && fj.MessageType == MessageType.Publish));

//            _mockStorageConnection
//                .Setup(m => m.FetchNextMessageAsync())
//                .ReturnsAsync(mockFetchedJob.Object).Verifiable();

//            _mockQueueExecutor
//                .Setup(x => x.ExecuteAsync(_mockStorageConnection.Object, mockFetchedJob.Object))
//                .Returns(Task.FromResult(OperateResult.Success));

//            var fixture = Create();

//            // Act
//            await fixture.ProcessAsync(_context);

//            // Assert
//            _mockStorageConnection.VerifyAll();
//        }

//        private DefaultDispatcher Create()
//            => _provider.GetService<DefaultDispatcher>();
//    }
//}