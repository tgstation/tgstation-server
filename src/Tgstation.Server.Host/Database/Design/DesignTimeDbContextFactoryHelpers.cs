using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database.Design
{
	/// <summary>
	/// Contains helpers for creating design time <see cref="DatabaseContext"/>s
	/// </summary>
	static class DesignTimeDbContextFactoryHelpers
	{
		/// <summary>
		/// Get the <see cref="IOptions{TOptions}"/> for the <see cref="DatabaseConfiguration"/>
		/// </summary>
		/// <typeparam name="TDatabaseContext">The <see cref="DatabaseContext"/> to create <see cref="DbContextOptions"/> for.</typeparam>
		/// <param name="databaseType">The <see cref="DatabaseConfiguration.DatabaseType"/>.</param>
		/// <param name="connectionString">The <see cref="DatabaseConfiguration.ConnectionString"/>.</param>
		/// <returns>The <see cref="IOptions{TOptions}"/> for the <see cref="DatabaseConfiguration"/></returns>
		public static DbContextOptions<TDatabaseContext> CreateDatabaseContextOptions<TDatabaseContext>(
			DatabaseType databaseType,
			string connectionString)
			where TDatabaseContext : DatabaseContext
		{
			var dbConfig = new DatabaseConfiguration
			{
				DesignTime = true,
				DatabaseType = databaseType,
				ConnectionString = connectionString
			};

			var optionsFac = new DbContextOptionsBuilder<TDatabaseContext>();
			var configureAction = DatabaseContext.GetConfigureAction<TDatabaseContext>();

			configureAction(optionsFac, dbConfig);

			return optionsFac.Options;
		}
	}
}
