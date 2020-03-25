using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Database.Design
{
	/// <inheritdoc />
	sealed class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteDatabaseContext>
	{
		/// <inheritdoc />
		public SqliteDatabaseContext CreateDbContext(string[] args)
		{
			using (var loggerFactory = new LoggerFactory())
			{
				return new SqliteDatabaseContext(
					new DbContextOptions<SqliteDatabaseContext>(),
					DesignTimeDbContextFactoryHelpers.GetDbContextOptions(),
					new DatabaseSeeder(
						new CryptographySuite(
							new PasswordHasher<User>())),
					loggerFactory.CreateLogger<SqliteDatabaseContext>());
			}
		}
	}
}
