using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// For initially setting up a database.
	/// </summary>
	interface IDatabaseSeeder
	{
		/// <summary>
		/// Setup up a given <paramref name="databaseContext"/>.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to setup.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Initialize(IDatabaseContext databaseContext, CancellationToken cancellationToken);

		/// <summary>
		/// Migrate a given <paramref name="databaseContext"/> down.
		/// </summary>
		/// <param name="databaseContext">The <see cref="IDatabaseContext"/> to downgrade.</param>
		/// <param name="downgradeVersion">The migration <see cref="Version"/> to downgrade the <paramref name="databaseContext"/> to.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task Downgrade(IDatabaseContext databaseContext, Version downgradeVersion, CancellationToken cancellationToken);
	}
}
