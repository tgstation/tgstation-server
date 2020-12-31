﻿using Microsoft.EntityFrameworkCore.Design;
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
			SqliteDatabaseContext.DesignTime = true;
			return new SqliteDatabaseContext(
				DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<SqliteDatabaseContext>(
					DatabaseType.Sqlite,
					"Data Source=tgs_design.sqlite3;Mode=ReadWriteCreate"));
		}
	}
}
