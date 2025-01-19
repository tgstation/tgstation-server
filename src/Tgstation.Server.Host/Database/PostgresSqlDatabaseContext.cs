using System;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for PostgresSQL.
	/// </summary>
	sealed class PostgresSqlDatabaseContext : DatabaseContext
	{
		/// <inheritdoc />
		protected override DeleteBehavior RevInfoCompileJobDeleteBehavior => DeleteBehavior.Cascade;

		/// <summary>
		/// Initializes a new instance of the <see cref="PostgresSqlDatabaseContext"/> class.
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/>.</param>
		public PostgresSqlDatabaseContext(
			DbContextOptions<PostgresSqlDatabaseContext> dbContextOptions)
			: base(dbContextOptions)
		{
		}

		/// <summary>
		/// Configure the <see cref="PostgresSqlDatabaseContext"/>.
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptionsBuilder"/> to configure.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/>.</param>
		public static void ConfigureWith(DbContextOptionsBuilder options, DatabaseConfiguration databaseConfiguration)
		{
			ArgumentNullException.ThrowIfNull(options);
			ArgumentNullException.ThrowIfNull(databaseConfiguration);

			if (databaseConfiguration.DatabaseType != DatabaseType.PostgresSql)
				throw new InvalidOperationException($"Invalid DatabaseType for {nameof(PostgresSqlDatabaseContext)}!");

			// Why the fuck is this an AppContext switch
			AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
			options.UseNpgsql(databaseConfiguration.ConnectionString, options =>
			{
				options.TranslateParameterizedCollectionsToConstants();
				options.EnableRetryOnFailure();
				options.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);

				if (!String.IsNullOrEmpty(databaseConfiguration.ServerVersion))
					options.SetPostgresVersion(
						Version.Parse(databaseConfiguration.ServerVersion));
			});
		}
	}
}
