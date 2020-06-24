using Microsoft.EntityFrameworkCore.Design;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database.Design
{
	/// <summary>
	/// <see cref="IDesignTimeDbContextFactory{TContext}"/> for creating <see cref="SqlServerDatabaseContext"/>s.
	/// </summary>
	sealed class SqlServerDesignTimeDbContextFactory : IDesignTimeDbContextFactory<SqlServerDatabaseContext>
	{
		/// <inheritdoc />
		public SqlServerDatabaseContext CreateDbContext(string[] args)
			=> new SqlServerDatabaseContext(
				DesignTimeDbContextFactoryHelpers.CreateDatabaseContextOptions<SqlServerDatabaseContext>(
					DatabaseType.SqlServer,
					"Data Source=fake;Initial Catalog=TGS_Design;Integrated Security=True;Application Name=tgstation-server"));
	}
}
