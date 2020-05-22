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
	/// <see cref="IDesignTimeDbContextFactory{TContext}"/> for creating <see cref="SqlServerDatabaseContext"/>s.
	/// </summary>
	sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlServerDatabaseContext>
	{
		/// <inheritdoc />
		public SqlServerDatabaseContext CreateDbContext(string[] args)
		{
			using var loggerFactory = new LoggerFactory();
			return new SqlServerDatabaseContext(
				new DbContextOptions<SqlServerDatabaseContext>(),
				DesignTimeDbContextFactoryHelpers.GetDbContextOptions(
					DatabaseType.SqlServer,
					"Data Source=fake;Initial Catalog=TGS_Design;Integrated Security=True;Application Name=tgstation-server"),
				new DatabaseSeeder(
				new CryptographySuite(
				new PasswordHasher<User>()),
				new PlatformIdentifier()),
				loggerFactory.CreateLogger<SqlServerDatabaseContext>());
		}
	}
}
