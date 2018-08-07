using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <inheritdoc />
	sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlServerDatabaseContext>
	{
		/// <inheritdoc />
		public SqlServerDatabaseContext CreateDbContext(string[] args) => new SqlServerDatabaseContext(new DbContextOptions<SqlServerDatabaseContext>(), DesignTimeDbContextFactoryHelpers.GetDbContextOptions(), new DatabaseSeeder(new CryptographySuite(new PasswordHasher<User>())), new LoggerFactory().CreateLogger<SqlServerDatabaseContext>());
	}
}
