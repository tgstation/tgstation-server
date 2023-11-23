using Microsoft.EntityFrameworkCore.Design;

using Tgstation.Server.Host.Configuration;

#nullable disable

namespace Tgstation.Server.Host.Database.Design
{
	/// <summary>
	/// <see cref="IDesignTimeDbContextFactory{TContext}"/> for creating <see cref="MySqlDatabaseContext"/>s.
	/// </summary>
	sealed class MySqlDesignTimeDbContextFactory : IDesignTimeDbContextFactory<MySqlDatabaseContext>
	{
		/// <inheritdoc />
		public MySqlDatabaseContext CreateDbContext(string[] args)
			=> new MySqlDatabaseContext(
				DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<MySqlDatabaseContext>(
					DatabaseType.MariaDB,
					"Server=127.0.0.1;User Id=root;Password=zdxfOOTlQFnklwzytzCj;Database=TGS_Design"));
	}
}
