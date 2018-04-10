using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for Sqlserver
	/// </summary>
	sealed class SqlServerDatabaseContext : DatabaseContext<SqlServerDatabaseContext>
	{
		/// <summary>
		/// Construct a <see cref="SqlServerDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="loggerFactory">The <see cref="ILoggerFactory"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext"/></param>
		public SqlServerDatabaseContext(DbContextOptions<SqlServerDatabaseContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfiguration, ILoggerFactory loggerFactory, IDatabaseSeeder databaseSeeder) : base(dbContextOptions, databaseConfiguration, loggerFactory, databaseSeeder)
		{ }

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			base.OnConfiguring(options);
			options.UseSqlServer(ConnectionString);
		}
	}
}
