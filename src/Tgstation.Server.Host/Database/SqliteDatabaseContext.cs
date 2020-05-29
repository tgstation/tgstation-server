using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// <see cref="DatabaseContext"/> for MySQL
	/// </summary>
	sealed class SqliteDatabaseContext : DatabaseContext
	{
		/// <summary>
		/// Construct a <see cref="MySqlDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext"/></param>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="DatabaseContext"/></param>
		public SqliteDatabaseContext(DbContextOptions<SqliteDatabaseContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfiguration, IDatabaseSeeder databaseSeeder, ILogger<SqliteDatabaseContext> logger) : base(dbContextOptions, databaseConfiguration, databaseSeeder, logger)
		{ }

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			base.OnConfiguring(options);
			options.UseSqlite(DatabaseConfiguration.ConnectionString);
		}

		/// <inheritdoc />
		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			// Shamelessly stolen from https://blog.dangl.me/archive/handling-datetimeoffset-in-sqlite-with-entity-framework-core/

			// SQLite does not have proper support for DateTimeOffset via Entity Framework Core, see the limitations
			// here: https://docs.microsoft.com/en-us/ef/core/providers/sqlite/limitations#query-limitations
			// To work around this, when the Sqlite database provider is used, all model properties of type DateTimeOffset
			// use the DateTimeOffsetToBinaryConverter
			// Based on: https://github.com/aspnet/EntityFrameworkCore/issues/10784#issuecomment-415769754
			// This only supports millisecond precision, but should be sufficient for most use cases.
			if (!DatabaseConfiguration.DesignTime)
				foreach (var entityType in modelBuilder.Model.GetEntityTypes())
				{
					var properties = entityType
						.ClrType
						.GetProperties()
						.Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
					foreach (var property in properties)
						modelBuilder
							.Entity(entityType.Name)
							.Property(property.Name)
							.HasConversion(new DateTimeOffsetToBinaryConverter());
				}
		}

		/// <inheritdoc />
		protected override void ValidateDatabaseType()
		{
			if (DatabaseType != DatabaseType.Sqlite)
				throw new InvalidOperationException("Invalid DatabaseType for SqliteDatabaseContext!");
		}
	}
}
