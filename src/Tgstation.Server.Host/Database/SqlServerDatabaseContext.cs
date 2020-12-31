using Microsoft.EntityFrameworkCore;
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
		public SqlServerDatabaseContext(DbContextOptions<SqlServerDatabaseContext> dbContextOptions) : base(dbContextOptions)
		{ }

		/// <summary>
		/// Configure the <see cref="SqlServerDatabaseContext"/>.
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptionsBuilder"/> to configure.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/>.</param>
		public static void ConfigureWith(DbContextOptionsBuilder options, DatabaseConfiguration databaseConfiguration)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (databaseConfiguration == null)
				throw new ArgumentNullException(nameof(databaseConfiguration));

			if (databaseConfiguration.DatabaseType != DatabaseType.SqlServer)
				throw new InvalidOperationException($"Invalid DatabaseType for {nameof(SqlServerDatabaseContext)}!");

			options.UseSqlServer(databaseConfiguration.ConnectionString, x => x.EnableRetryOnFailure());
		}
	}
}
