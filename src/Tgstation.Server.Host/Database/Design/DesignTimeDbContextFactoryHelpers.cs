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
		/// <param name="databaseType">The <see cref="DatabaseConfiguration.DatabaseType"/>.</param>
		/// <param name="connectionString">The <see cref="DatabaseConfiguration.ConnectionString"/>.</param>
		/// <returns>The <see cref="IOptions{TOptions}"/> for the <see cref="DatabaseConfiguration"/></returns>
		public static IOptions<DatabaseConfiguration> GetDatabaseConfiguration(DatabaseType databaseType, string connectionString)
		{
			var dbConfig = new DatabaseConfiguration
			{
				DesignTime = true,
				DatabaseType = databaseType,
				ConnectionString = connectionString
			};

			return Options.Create(dbConfig);
		}
	}
}
