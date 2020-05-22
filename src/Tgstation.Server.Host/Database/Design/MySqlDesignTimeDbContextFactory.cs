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
	/// <see cref="IDesignTimeDbContextFactory{TContext}"/> for creating <see cref="MySqlDatabaseContext"/>s.
	/// </summary>
	sealed class MySqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MySqlDatabaseContext>
	{
		/// <inheritdoc />
		public MySqlDatabaseContext CreateDbContext(string[] args)
		{
			using var loggerFactory = new LoggerFactory();
			return new MySqlDatabaseContext(
				new DbContextOptions<MySqlDatabaseContext>(),
				DesignTimeDbContextFactoryHelpers.GetDbContextOptions(
					DatabaseType.MariaDB,
					"Server=127.0.0.1;User Id=root;Password=fake;Database=TGS_Design"),
				new DatabaseSeeder(
				new CryptographySuite(
				new PasswordHasher<User>()),
				new PlatformIdentifier()),
				loggerFactory.CreateLogger<MySqlDatabaseContext>());
		}
	}
}
