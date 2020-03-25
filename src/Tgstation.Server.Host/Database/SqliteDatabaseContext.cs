using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext{TParentContext}"/> for MySQL
	/// </summary>
	sealed class SqliteDatabaseContext : DatabaseContext<SqliteDatabaseContext>
	{
		/// <inheritdoc />
		protected override DatabaseType DatabaseType => DatabaseType.Sqlite;

		/// <summary>
		/// Construct a <see cref="MySqlDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		public SqliteDatabaseContext(DbContextOptions<SqliteDatabaseContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfiguration, IDatabaseSeeder databaseSeeder, ILogger<SqliteDatabaseContext> logger) : base(dbContextOptions, databaseConfiguration, databaseSeeder, logger)
		{ }

		/// <inheritdoc />
		public override Task Initialize(CancellationToken cancellationToken) => throw new NotImplementedException("SQLite support currently is incomplete. See tracking issue at https://github.com/tgstation/tgstation-server/issues/885");

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			base.OnConfiguring(options);
			options.UseSqlite(DatabaseConfiguration.ConnectionString);
		}
	}
}
