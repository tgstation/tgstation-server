using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class DatabaseContextFactory : IDatabaseContextFactory
	{
		/// <summary>
		/// The <see cref="IServiceProvider"/> for the <see cref="DatabaseContextFactory"/>
		/// </summary>
		readonly IServiceProvider serviceProvider;

		/// <summary>
		/// Construct a <see cref="DatabaseContextFactory"/>
		/// </summary>
		/// <param name="serviceProvider">The value of <see cref="serviceProvider"/></param>
		public DatabaseContextFactory(IServiceProvider serviceProvider) => this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

		/// <inheritdoc />
		public async Task UseContext(Func<IDatabaseContext, Task> operation)
		{
			using (var scope = serviceProvider.CreateScope())
				await operation(scope.ServiceProvider.GetRequiredService<IDatabaseContext>()).ConfigureAwait(false);
		}
	}
}
