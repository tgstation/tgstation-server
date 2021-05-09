using System.Data.Common;

using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// For creating raw <see cref="DbConnection"/>s.
	/// </summary>
	interface IDatabaseConnectionFactory
	{
		/// <summary>
		/// Create a <see cref="DbConnection"/>.
		/// </summary>
		/// <param name="connectionString">The <see cref="DbConnection.ConnectionString"/>.</param>
		/// <param name="databaseType">The <see cref="DatabaseType"/> to create.</param>
		/// <returns>A new <see cref="DbConnection"/>.</returns>
		DbConnection CreateConnection(string connectionString, DatabaseType databaseType);
	}
}
