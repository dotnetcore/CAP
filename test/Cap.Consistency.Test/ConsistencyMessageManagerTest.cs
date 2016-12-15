using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Cap.Consistency.Test
{
    public class ConsistencyMessageManagerTest
    {
        [Fact]
        public void EnsureDefaultServicesDefaultsWithStoreWorks() {
            var services = new ServiceCollection().AddTransient<IConsistencyMessageStore<TestConsistencyMessage>, NoopMessageStore>();
        }
    }
}
