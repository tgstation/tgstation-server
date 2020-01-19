using MySql.Data.MySqlClient;
using System;
using System.Data.Common;
using System.Data.SqlClient;
using Tgstation.Server.Host.Configuration;

namespace Tgstation.Server.Host.Core
{
	/// <inheritdoc />
	sealed class DatabaseConnectionFactory : IDatabaseConnectionFactory
	{
		/// <inheritdoc />
		public DbConnection CreateConnection(string connectionString, DatabaseType databaseType)
		{
			if (connectionString == null)
				throw new ArgumentNullException(nameof(connectionString));

			switch (databaseType)
			{
				case DatabaseType.MariaDB:
				case DatabaseType.MySql:
					return new MySqlConnection
					{
						ConnectionString = connectionString
					};
				case DatabaseType.SqlServer:
					return new SqlConnection
					{
						ConnectionString = connectionString
					};
				default:
					throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, "Invalid DatabaseType!");
			}
		}
	}
}
