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
	/// <see cref="DatabaseContext{TParentContext}"/> for MySQL
	/// </summary>
	sealed class MySqlDatabaseContext : DatabaseContext<MySqlDatabaseContext>
	{
		/// <inheritdoc />
		protected override DatabaseType DatabaseType => DatabaseType.MySql;

		/// <summary>
		/// Construct a <see cref="MySqlDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
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
			if (!String.IsNullOrEmpty(DatabaseConfiguration.MySqlServerVersion))
				options.UseMySql(DatabaseConfiguration.ConnectionString, mySqlOptions => mySqlOptions.ServerVersion(Version.Parse(DatabaseConfiguration.MySqlServerVersion), DatabaseConfiguration.DatabaseType == DatabaseType.MariaDB ? ServerType.MariaDb : ServerType.MySql));
			else
				options.UseMySql(DatabaseConfiguration.ConnectionString);
		}
	}
}
