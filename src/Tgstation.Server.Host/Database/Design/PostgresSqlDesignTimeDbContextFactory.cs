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
	/// <inheritdoc />
	sealed class PostgresSqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<PostgresSqlDatabaseContext>
	{
		/// <inheritdoc />
		public PostgresSqlDatabaseContext CreateDbContext(string[] args)
		{
			using var loggerFactory = new LoggerFactory();
			return new PostgresSqlDatabaseContext(
				new DbContextOptions<PostgresSqlDatabaseContext>(),
				DesignTimeDbContextFactoryHelpers.GetDatabaseConfiguration(
					DatabaseType.PostgresSql,
					"Application Name=tgstation-server;Host=127.0.0.1;Password=qCkWimNgLfWwpr7TnUHs;Username=postgres;Database=TGS_Design"),
				new DatabaseSeeder(
				new CryptographySuite(
				new PasswordHasher<User>()),
				new PlatformIdentifier()),
				loggerFactory.CreateLogger<PostgresSqlDatabaseContext>());
		}
	}
}
