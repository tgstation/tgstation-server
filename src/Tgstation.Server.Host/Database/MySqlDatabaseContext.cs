using System;

using Microsoft.EntityFrameworkCore;

using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for MySQL.
	/// </summary>
	sealed class MySqlDatabaseContext : DatabaseContext
	{
		/// <inheritdoc />
		protected override DeleteBehavior RevInfoCompileJobDeleteBehavior => DeleteBehavior.Cascade;

		/// <summary>
		/// Initializes a new instance of the <see cref="MySqlDatabaseContext"/> class.
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/>.</param>
		public MySqlDatabaseContext(DbContextOptions<MySqlDatabaseContext> dbContextOptions) : base(dbContextOptions)
		{
		}

		/// <summary>
		/// Configure the <see cref="MySqlDatabaseContext"/>.
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptionsBuilder"/> to configure.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/>.</param>
		public static void ConfigureWith(DbContextOptionsBuilder options, DatabaseConfiguration databaseConfiguration)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (databaseConfiguration == null)
				throw new ArgumentNullException(nameof(databaseConfiguration));

			if (databaseConfiguration.DatabaseType != DatabaseType.MariaDB && databaseConfiguration.DatabaseType != DatabaseType.MySql)
				throw new InvalidOperationException($"Invalid DatabaseType for {nameof(MySqlDatabaseContext)}!");

			ServerVersion serverVersion;
			if (!String.IsNullOrEmpty(databaseConfiguration.ServerVersion))
			{
				serverVersion = ServerVersion.Parse(
					databaseConfiguration.ServerVersion,
					databaseConfiguration.DatabaseType == DatabaseType.MariaDB
						? ServerType.MariaDb
				: ServerType.MySql);
			}
			else
				serverVersion = ServerVersion.AutoDetect(databaseConfiguration.ConnectionString);

			options.UseMySql(
				databaseConfiguration.ConnectionString,
				serverVersion,
				mySqlOptions =>
				{
					mySqlOptions.EnableRetryOnFailure();
					mySqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
				});
		}
	}
}
