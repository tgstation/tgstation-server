using Microsoft.EntityFrameworkCore;
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
		public MySqlDatabaseContext(DbContextOptions<MySqlDatabaseContext> dbContextOptions) : base(dbContextOptions)
		{ }

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

			options.UseMySql(
				databaseConfiguration.ConnectionString,
				mySqlOptions =>
				{
					mySqlOptions.EnableRetryOnFailure();

					if (!String.IsNullOrEmpty(databaseConfiguration.ServerVersion))
						Console.WriteLine("SQLVERSION: " + databaseConfiguration.DatabaseType);
						mySqlOptions.ServerVersion(
							Version.Parse(databaseConfiguration.ServerVersion),
							databaseConfiguration.DatabaseType == DatabaseType.MariaDB
								? ServerType.MariaDb
								: ServerType.MySql);
				});
		}
	}
}
