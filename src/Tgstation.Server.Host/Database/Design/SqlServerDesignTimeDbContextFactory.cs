using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Database.Design
{
	/// <inheritdoc />
	sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlServerDatabaseContext>
	{
		/// <inheritdoc />
		public SqlServerDatabaseContext CreateDbContext(string[] args)
		{
			using (var loggerFactory = new LoggerFactory())
			{
				return new SqlServerDatabaseContext(
					new DbContextOptions<SqlServerDatabaseContext>(),
					DesignTimeDbContextFactoryHelpers.GetDbContextOptions(),
					new DatabaseSeeder(
						new CryptographySuite(
							new PasswordHasher<User>()),
						new PlatformIdentifier()),
					loggerFactory.CreateLogger<SqlServerDatabaseContext>());
			}
		}
	}
}
