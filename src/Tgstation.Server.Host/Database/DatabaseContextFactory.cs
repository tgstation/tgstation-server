using System;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	sealed class DatabaseContextFactory : IDatabaseContextFactory
	{
		/// <summary>
		/// The <see cref="IServiceScopeFactory"/> for the <see cref="DatabaseContextFactory"/>.
		/// </summary>
		readonly IServiceScopeFactory scopeFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseContextFactory"/> class.
		/// </summary>
		/// <param name="scopeFactory">The value of <see cref="scopeFactory"/>. Created scopes must be able to provide instances of <see cref="IDatabaseContext"/>.</param>
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

			await using var scope = scopeFactory.CreateAsyncScope();
			await operation(scope.ServiceProvider.GetRequiredService<IDatabaseContext>());
		}
	}
}
