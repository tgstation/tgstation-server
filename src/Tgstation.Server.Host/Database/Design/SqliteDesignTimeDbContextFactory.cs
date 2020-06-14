using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database.Design
{
	/// <summary>
	/// <see cref="IDesignTimeDbContextFactory{TContext}"/> for creating <see cref="SqliteDatabaseContext"/>s.
	/// </summary>
	sealed class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteDatabaseContext>
	{
		/// <inheritdoc />
		public SqliteDatabaseContext CreateDbContext(string[] args)
		{
			using var loggerFactory = new LoggerFactory();
			var config =
				DesignTimeDbContextFactoryHelpers.GetDatabaseConfiguration(
					DatabaseType.Sqlite,
					"Data Source=tgs_design.sqlite3;Mode=ReadWriteCreate");
			SqliteDatabaseContext.DesignTime = config.Value.DesignTime;
			return new SqliteDatabaseContext(
				new DbContextOptions<SqliteDatabaseContext>());
		}
	}
}
