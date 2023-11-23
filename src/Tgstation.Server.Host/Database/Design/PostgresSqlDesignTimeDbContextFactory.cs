using Microsoft.EntityFrameworkCore.Design;

using Tgstation.Server.Common;
using Tgstation.Server.Host.Configuration;

#nullable disable

namespace Tgstation.Server.Host.Database.Design
{
	/// <summary>
	/// <see cref="IDesignTimeDbContextFactory{TContext}"/> for creating <see cref="PostgresSqlDatabaseContext"/>s.
	/// </summary>
	sealed class PostgresSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresSqlDatabaseContext>
	{
		/// <inheritdoc />
		public PostgresSqlDatabaseContext CreateDbContext(string[] args)
			=> new PostgresSqlDatabaseContext(
				DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<PostgresSqlDatabaseContext>(
					DatabaseType.PostgresSql,
					$"Application Name={Constants.CanonicalPackageName};Host=127.0.0.1;Password=fake;Username=postgres;Database=TGS_Design"));
	}
}
