using Microsoft.EntityFrameworkCore.Design;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database.Design
{
	/// <summary>
	/// <see cref="IDesignTimeDbContextFactory{TContext}"/> for creating <see cref="MySqlDatabaseContext"/>s.
	/// </summary>
	sealed class MySqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MySqlDatabaseContext>
	{
		/// <inheritdoc />
		public MySqlDatabaseContext CreateDbContext(string[] args)
			=> new(
				DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<MySqlDatabaseContext>(
					DatabaseType.MariaDB,
					"Server=127.0.0.1;User Id=root;Password=zdxfOOTlQFnklwzytzCj;Database=TGS_Design"));
	}
}
