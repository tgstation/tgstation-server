using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// <see cref="DatabaseContext{TParentContext}"/> for Sqlite
	/// </summary>
	sealed class SqliteDatabaseContext : DatabaseContext<SqliteDatabaseContext>
	{
		/// <summary>
		/// Construct a <see cref="SqliteDatabaseContext"/>
		/// </summary>
		/// <param name="dbContextOptions">The <see cref="DbContextOptions{TContext}"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseConfiguration">The <see cref="IOptions{TOptions}"/> of <see cref="DatabaseConfiguration"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		/// <param name="databaseSeeder">The <see cref="IDatabaseSeeder"/> for the <see cref="DatabaseContext{TParentContext}"/></param>
		public SqliteDatabaseContext(DbContextOptions<SqliteDatabaseContext> dbContextOptions, IOptions<DatabaseConfiguration> databaseConfiguration, IDatabaseSeeder databaseSeeder) : base(dbContextOptions, databaseConfiguration, databaseSeeder)
		{ }

		/// <inheritdoc />
		protected override void OnConfiguring(DbContextOptionsBuilder options)
		{
			base.OnConfiguring(options);
			options.UseSqlite(ConnectionString);
		}
	}
}
