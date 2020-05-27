using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext{TParentContext}"/> for PostgresSQL.
	/// </summary>
	sealed class PostgresSqlDatabaseContext : DatabaseContext<PostgresSqlDatabaseContext>
	{
		/// <summary>
		/// Construct a <see cref="SqlServerDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		public PostgresSqlDatabaseContext(
			DbContextOptions<PostgresSqlDatabaseContext> dbContextOptions,
			IOptions<DatabaseConfiguration> databaseConfiguration,
			IDatabaseSeeder databaseSeeder,
			ILogger<PostgresSqlDatabaseContext> logger)
			: base(dbContextOptions, databaseConfiguration, databaseSeeder, logger)
		{ }

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			base.OnConfiguring(options);
			options.UseNpgsql(DatabaseConfiguration.ConnectionString, x => x.EnableRetryOnFailure());
		}

		/// <inheritdoc />
		protected override void ValidateDatabaseType()
		{
			if (!Debugger.IsAttached)
				throw new NotImplementedException("PostgresSQL implementation is not complete yet!");

			if (DatabaseType != DatabaseType.PostgresSql)
				throw new InvalidOperationException("Invalid DatabaseType for SqliteDatabaseContext!");
		}
	}
}
