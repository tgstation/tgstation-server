using System;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for Sqlserver.
	/// </summary>
	sealed class SqlServerDatabaseContext : DatabaseContext
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SqlServerDatabaseContext"/> class.
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/>.</param>
		public SqlServerDatabaseContext(DbContextOptions<SqlServerDatabaseContext> dbContextOptions)
			: base(dbContextOptions)
		{
		}

		/// <summary>
		/// Configure the <see cref="SqlServerDatabaseContext"/>.
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptionsBuilder"/> to configure.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/>.</param>
		public static void ConfigureWith(DbContextOptionsBuilder options, DatabaseConfiguration databaseConfiguration)
		{
			ArgumentNullException.ThrowIfNull(options);
			ArgumentNullException.ThrowIfNull(databaseConfiguration);

			if (databaseConfiguration.DatabaseType != DatabaseType.SqlServer)
				throw new InvalidOperationException($"Invalid DatabaseType for {nameof(SqlServerDatabaseContext)}!");

#if NET7_0_OR_GREATER
#error Perform this breaking config change
#endif

			// Workaround for breaking change https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/breaking-changes#encrypt-true
			var connectionString = databaseConfiguration.ConnectionString;
			if (!connectionString.Contains("Encrypt=", StringComparison.OrdinalIgnoreCase))
			{
				var connectionStringBuilder = new SqlConnectionStringBuilder(databaseConfiguration.ConnectionString)
				{
					Encrypt = false,
				};

				connectionString = connectionStringBuilder.ToString();
			}

			options.UseSqlServer(
				connectionString,
				sqlServerOptions =>
				{
					sqlServerOptions.EnableRetryOnFailure();
					sqlServerOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
				});
		}
	}
}
