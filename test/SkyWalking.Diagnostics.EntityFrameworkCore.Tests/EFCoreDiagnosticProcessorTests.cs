using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SkyWalking.Diagnostics.EntityFrameworkCore.Tests.Fakes;
using Xunit;

namespace SkyWalking.Diagnostics.EntityFrameworkCore.Tests
{
    // ReSharper disable once InconsistentNaming
    public class EFCoreDiagnosticProcessorTests : IDisposable
    {
        private readonly DbContextOptions<FakeDbContext> _options;

        public EFCoreDiagnosticProcessorTests()
        {
            // In-memory database only exists while the connection is open
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();

            _options = new DbContextOptionsBuilder<FakeDbContext>()
                .UseSqlite(connection)
                .Options;
            
            using (var dbContext = new FakeDbContext(_options))
            {
                dbContext.Database.EnsureCreated();
            }
        }

        [Fact]
        public void DbContext_Init_Success_Test()
        {
            using (var dbContext = new FakeDbContext(_options))
            {
                Assert.NotNull(dbContext);
            }
        }

        [Fact]
        public void DbContext_Works_Success_Test()
        {
            using (var dbContext = new FakeDbContext(_options))
            {
                dbContext.Users.Add(new FakeUser("Zhang", "San"));
                dbContext.SaveChanges();
            }

            using (var dbContext = new FakeDbContext(_options))
            {
                Assert.Single(dbContext.Users);
            }
        }

        [Fact]
        public void EF_Diagnostics_Success_Test()
        {
            var processorObserver = new TracingDiagnosticProcessorObserver(new[]
            {
                new EntityFrameworkCoreDiagnosticProcessor(new EfCoreSpanFactory(new List<IEfCoreSpanMetadataProvider>()))
            });

            DiagnosticListener.AllListeners.Subscribe(processorObserver);

            using (var tracerContextListener = new FakeIgnoreTracerContextListener())
            {
                try
                {
                    using (var dbContext = new FakeDbContext(_options))
                    {
                        dbContext.Users.Add(new FakeUser("Zhang", "San"));
                        dbContext.SaveChanges();
                    }
                }
                catch
                {
                    // ignored
                }
                Assert.Equal(1, tracerContextListener.Counter);
            }
        }


        public void Dispose()
        {
            using (var context = new FakeDbContext(_options))
            {
                context.Database.EnsureDeleted();
            }
        }
    }
}
