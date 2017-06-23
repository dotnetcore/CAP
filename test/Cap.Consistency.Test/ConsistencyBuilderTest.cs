//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Cap.Consistency.Infrastructure;
//using Cap.Consistency.Store;
//using Microsoft.Extensions.DependencyInjection;
//using Xunit;

//namespace Cap.Consistency.Test
//{
//    public class ConsistencyBuilderTest
//    {
//        [Fact]
//        public void CanOverrideMessageStore() {
//            var services = new ServiceCollection();
//            services.AddConsistency().AddMessageStore<MyUberThingy>();
//            var thingy = services.BuildServiceProvider().GetRequiredService<IConsistencyMessageStore>() as MyUberThingy;
//            Assert.NotNull(thingy);
//        }

//        private class MyUberThingy : IConsistencyMessageStore
//        {
//            public Task<OperateResult> CreateAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
//                throw new NotImplementedException();
//            }

//            public Task<OperateResult> DeleteAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
//                throw new NotImplementedException();
//            }

//            public void Dispose() {
//                throw new NotImplementedException();
//            }

//            public Task<ConsistencyMessage> FindByIdAsync(string messageId, CancellationToken cancellationToken) {
//                throw new NotImplementedException();
//            }

//            public Task<string> GeConsistencyMessageIdAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
//                throw new NotImplementedException();
//            }

//            public Task<string> GetMessageIdAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
//                throw new NotImplementedException();
//            }

//            public Task<OperateResult> UpdateAsync(ConsistencyMessage message, CancellationToken cancellationToken) {
//                throw new NotImplementedException();
//            }
//        }
//    }
//}