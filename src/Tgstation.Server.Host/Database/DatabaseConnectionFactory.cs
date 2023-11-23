using System;
using System.Data.Common;

using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;

using MySqlConnector;

using Npgsql;

using Tgstation.Server.Host.Configuration;

#nullable disable

namespace Tgstation.Server.Host.Database
{
	/// <inheritdoc />
	sealed class DatabaseConnectionFactory : IDatabaseConnectionFactory
	{
		/// <inheritdoc />
		public DbConnection CreateConnection(string connectionString, DatabaseType databaseType)
		{
			ArgumentNullException.ThrowIfNull(connectionString);

			return databaseType switch
			{
				DatabaseType.MariaDB or DatabaseType.MySql => new MySqlConnection(connectionString),
				DatabaseType.SqlServer => new SqlConnection(connectionString),
				DatabaseType.Sqlite => new SqliteConnection(connectionString),
				DatabaseType.PostgresSql => new NpgsqlConnection(connectionString),
				_ => throw new ArgumentOutOfRangeException(nameof(databaseType), databaseType, "Invalid DatabaseType!"),
			};
		}
	}
}
