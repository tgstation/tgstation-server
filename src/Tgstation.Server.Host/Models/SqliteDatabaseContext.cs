using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// <see cref="DatabaseContext{TParentContext}"/> for Sqlite
	/// </summary>
	sealed class SqliteDatabaseContext : DatabaseContext<SqliteDatabaseContext>
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="SqliteDatabaseContext"/>
		/// </summary>
		readonly ILogger<SqliteDatabaseContext> logger;

		/// <summary>
		/// Construct a <see cref="SqliteDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		public SqliteDatabaseContext(DbContextOptions<SqliteDatabaseContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfiguration, IDatabaseSeeder databaseSeeder, ILogger<SqliteDatabaseContext> logger) : base(dbContextOptions, databaseConfiguration, databaseSeeder)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			base.OnConfiguring(options);
			//on the off chance that connection string is null here we default to a db file next to the executable since this is the default database
			if (ConnectionString == null)
			{
				logger.LogWarning("No database configured! Defaulting to SQLite in the working directory!");
				options.UseSqlite("Data Source=TgsDatabase.db3");
			}
			else
				options.UseSqlite(ConnectionString);
		}
	}
}
