using System;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for SQLite.
	/// </summary>
	sealed class SqliteDatabaseContext : DatabaseContext
	{
		/// <summary>
		/// Static property to receive the configured value of <see cref="DatabaseConfiguration.DesignTime"/>.
		/// </summary>
		public static bool DesignTime { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SqliteDatabaseContext"/> class.
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/>.</param>
		public SqliteDatabaseContext(DbContextOptions<SqliteDatabaseContext> dbContextOptions) : base(dbContextOptions)
		{
		}

		/// <summary>
		/// Configure the <see cref="SqliteDatabaseContext"/>.
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptionsBuilder"/> to configure.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/>.</param>
		public static void ConfigureWith(DbContextOptionsBuilder options, DatabaseConfiguration databaseConfiguration)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (databaseConfiguration == null)
				throw new ArgumentNullException(nameof(databaseConfiguration));

			if (databaseConfiguration.DatabaseType != DatabaseType.Sqlite)
				throw new InvalidOperationException($"Invalid DatabaseType for {nameof(SqliteDatabaseContext)}!");

			DesignTime = databaseConfiguration.DesignTime;
			options.UseSqlite(databaseConfiguration.ConnectionString, builder => builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));
		}
	}
}
