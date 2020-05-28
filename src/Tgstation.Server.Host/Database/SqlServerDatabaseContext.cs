using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for Sqlserver
	/// </summary>
	sealed class SqlServerDatabaseContext : DatabaseContext
	{
		/// <summary>
		/// Construct a <see cref="SqlServerDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="DatabaseContext"/></param>
		public SqlServerDatabaseContext(DbContextOptions<SqlServerDatabaseContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfiguration, IDatabaseSeeder databaseSeeder, ILogger<SqlServerDatabaseContext> logger) : base(dbContextOptions, databaseConfiguration, databaseSeeder, logger)
		{ }

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			base.OnConfiguring(options);
			options.UseSqlServer(DatabaseConfiguration.ConnectionString, x => x.EnableRetryOnFailure());
		}

		/// <inheritdoc />
		protected override void ValidateDatabaseType()
		{
			if (DatabaseType != DatabaseType.SqlServer)
				throw new InvalidOperationException("Invalid DatabaseType for SqlServerDatabaseContext!");
		}
	}
}
