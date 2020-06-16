using Microsoft.EntityFrameworkCore.Design;
using Tgstation.Server.Host.Configuration;

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
					"Application Name=tgstation-server;Host=127.0.0.1;Password=fake;Username=postgres;Database=TGS_Design"));
	}
}
