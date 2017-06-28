using System.Threading.Tasks;
using DotNetCore.CAP.Infrastructure;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Testing.xunit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DotNetCore.CAP.EntityFrameworkCore.Test
{
    public class DefaultPocoTest : IClassFixture<ScratchDatabaseFixture>
    {
        private readonly ApplicationBuilder _builder;

        public DefaultPocoTest(ScratchDatabaseFixture fixture)
        {
            var services = new ServiceCollection();

            services
                .AddDbContext<CapDbContext>(o => o.UseSqlServer(fixture.ConnectionString))
                .AddConsistency()
                .AddEntityFrameworkStores<CapDbContext>();

            services.AddLogging();

            var provider = services.BuildServiceProvider();
            _builder = new ApplicationBuilder(provider);

            using (var scoped = provider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            using (var db = scoped.ServiceProvider.GetRequiredService<CapDbContext>())
            {
                db.Database.EnsureCreated();
            }
        }

        [ConditionalFact]
        [FrameworkSkipCondition(RuntimeFrameworks.Mono)]
        [OSSkipCondition(OperatingSystems.Linux)]
        [OSSkipCondition(OperatingSystems.MacOSX)]
        public async Task EnsureStartupUsageWorks()
        {
            var messageStore = _builder.ApplicationServices.GetRequiredService<ICapMessageStore>();
            var messageManager = _builder.ApplicationServices.GetRequiredService<ICapMessageStore>();

            Assert.NotNull(messageStore);
            Assert.NotNull(messageManager);

            var message = new CapSentMessage();

            var operateResult = await messageManager.StoreSentMessageAsync(message);
            Assert.True(operateResult.Succeeded);

            operateResult = await messageManager.RemoveSentMessageAsync(message);
            Assert.True(operateResult.Succeeded);
        }
    }
}