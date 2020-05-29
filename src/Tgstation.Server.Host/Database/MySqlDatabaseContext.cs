using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MySql.Data.MySqlClient;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;
using System;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for MySQL
	/// </summary>
	sealed class MySqlDatabaseContext : DatabaseContext
	{
		/// <inheritdoc />
		protected override DeleteBehavior RevInfoCompileJobDeleteBehavior => DeleteBehavior.Cascade;

		/// <summary>
		/// Construct a <see cref="MySqlDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="DatabaseContext"/></param>
		public MySqlDatabaseContext(DbContextOptions<MySqlDatabaseContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfiguration, IDatabaseSeeder databaseSeeder, ILogger<MySqlDatabaseContext> logger) : base(dbContextOptions, databaseConfiguration, databaseSeeder, logger)
		{ }

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			base.OnConfiguring(options);
			var stringDeconstructor = new MySqlConnectionStringBuilder
			{
				ConnectionString = DatabaseConfiguration.ConnectionString
			};
			if (stringDeconstructor.Server == "localhost")
				Logger.LogWarning("MariaDB/MySQL server address is set to 'localhost'! If there are connection issues, try setting it to '127.0.0.1'!");
			options.UseMySql(
				DatabaseConfiguration.ConnectionString,
				mySqlOptions =>
				{
					mySqlOptions.EnableRetryOnFailure();

					if (!String.IsNullOrEmpty(DatabaseConfiguration.MySqlServerVersion))
						mySqlOptions.ServerVersion(
							Version.Parse(DatabaseConfiguration.MySqlServerVersion),
							DatabaseConfiguration.DatabaseType == DatabaseType.MariaDB
								? ServerType.MariaDb
								: ServerType.MySql);
				});
		}

		/// <inheritdoc />
		protected override void ValidateDatabaseType()
		{
			if (DatabaseType != DatabaseType.MariaDB && DatabaseType != DatabaseType.MySql)
				throw new InvalidOperationException("Invalid DatabaseType for MySqlDatabaseContext!");
		}
	}
}
