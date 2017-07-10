using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DotNetCore.CAP.EntityFrameworkCore
{ 
    public class EFStorage : IStorage
	{
		private IServiceProvider _provider;
		private ILogger _logger;

		public EFStorage(
			IServiceProvider provider,
			ILogger<EFStorage> logger)
		{
			_provider = provider;
			_logger = logger;
		}

		public async Task InitializeAsync(CancellationToken cancellationToken)
		{
			using (var scope = _provider.CreateScope())
			{
				if (cancellationToken.IsCancellationRequested) return;

				var provider = scope.ServiceProvider;
				var context = provider.GetRequiredService<CapDbContext>();

				_logger.LogDebug("Ensuring all migrations are applied to Jobs database.");
				await context.Database.MigrateAsync(cancellationToken);
			}
		}
	}
}
