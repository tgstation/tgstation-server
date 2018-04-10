using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Logging;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Models.Migrations
{
	/// <inheritdoc />
	sealed class SqliteDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqliteDatabaseContext>
	{
		/// <inheritdoc />
		public SqliteDatabaseContext CreateDbContext(string[] args) => new SqliteDatabaseContext(new DbContextOptions<SqliteDatabaseContext>(), DesignTimeDbContextFactoryHelpers.GetDbContextOptions(), new LoggerFactory(), new DatabaseSeeder(new CryptographySuite(new PasswordHasher<User>())));
	}
}
