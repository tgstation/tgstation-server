using Microsoft.EntityFrameworkCore.Design;

using Tgstation.Server.Host.Configuration;

#nullable disable

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
			return new SqliteDatabaseContext(
				DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<SqliteDatabaseContext>(
					DatabaseType.Sqlite,
					"Data Source=tgs_design.sqlite3;Mode=ReadWriteCreate"),
				true);
		}
	}
}
