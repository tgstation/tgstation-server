using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Tgstation.Server.Host.Configuration;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

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
			return new SqliteDatabaseContext(
				new DbContextOptions<SqliteDatabaseContext>(),
				DesignTimeDbContextFactoryHelpers.GetDbContextOptions(
					DatabaseType.Sqlite,
					"Data Source=tgs_design.sqlite3;Mode=ReadWriteCreate"),
				new DatabaseSeeder(
				new CryptographySuite(
				new PasswordHasher<User>()),
				new PlatformIdentifier()),
				loggerFactory.CreateLogger<SqliteDatabaseContext>());
		}
	}
}
