using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	sealed class DatabaseContextFactory : IDatabaseContextFactory
	{
		/// <summary>
		/// The <see cref="IServiceScopeFactory"/> for the <see cref="DatabaseContextFactory"/>
		/// </summary>
		readonly IServiceScopeFactory scopeFactory;

		/// <summary>
		/// Construct a <see cref="DatabaseContextFactory"/>
		/// </summary>
		/// <param name="scopeFactory">The value of <see cref="scopeFactory"/>. Created scopes must be able to provide instances of <see cref="IDatabaseContext"/></param>
		public DatabaseContextFactory(IServiceScopeFactory scopeFactory)
		{
			this.scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

			using var scope = scopeFactory.CreateScope();
			scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
		}

		/// <inheritdoc />
		public async Task UseContext(Func<IDatabaseContext, Task> operation)
		{
			if (operation == null)
				throw new ArgumentNullException(nameof(operation));

			using var scope = scopeFactory.CreateScope();
			await operation(scope.ServiceProvider.GetRequiredService<IDatabaseContext>()).ConfigureAwait(false);
		}
	}
}
