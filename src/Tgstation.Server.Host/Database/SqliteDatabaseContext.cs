using System;
using System.Linq;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
			ArgumentNullException.ThrowIfNull(options);
			ArgumentNullException.ThrowIfNull(databaseConfiguration);

			if (databaseConfiguration.DatabaseType != DatabaseType.Sqlite)
				throw new InvalidOperationException($"Invalid DatabaseType for {nameof(SqliteDatabaseContext)}!");

			DesignTime = databaseConfiguration.DesignTime;
			options.UseSqlite(databaseConfiguration.ConnectionString, sqliteOptions => sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery));
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
			if (!DesignTime)
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
	}
}
