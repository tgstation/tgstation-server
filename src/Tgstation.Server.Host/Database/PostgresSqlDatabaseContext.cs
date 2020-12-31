﻿using Microsoft.EntityFrameworkCore;
using System;
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
		/// Construct a <see cref="SqlServerDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		public PostgresSqlDatabaseContext(
			DbContextOptions<PostgresSqlDatabaseContext> dbContextOptions)
			: base(dbContextOptions)
		{ }

		/// <summary>
		/// Configure the <see cref="PostgresSqlDatabaseContext"/>.
		/// </summary>
		/// <param name="options">The <see cref="DbContextOptionsBuilder"/> to configure.</param>
		/// <param name="databaseConfiguration">The <see cref="DatabaseConfiguration"/>.</param>
		public static void ConfigureWith(DbContextOptionsBuilder options, DatabaseConfiguration databaseConfiguration)
		{
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			if (databaseConfiguration == null)
				throw new ArgumentNullException(nameof(databaseConfiguration));

			if (databaseConfiguration.DatabaseType != DatabaseType.PostgresSql)
				throw new InvalidOperationException($"Invalid DatabaseType for {nameof(PostgresSqlDatabaseContext)}!");

			options.UseNpgsql(databaseConfiguration.ConnectionString, options =>
			{
				options.EnableRetryOnFailure();

				if (!String.IsNullOrEmpty(databaseConfiguration.ServerVersion))
					options.SetPostgresVersion(
						Version.Parse(databaseConfiguration.ServerVersion));
			});
		}
	}
}
