namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Type of database to user.
	/// </summary>
	public enum DatabaseType
	{
		/// <summary>
		/// Use Microsoft SQL Server.
		/// </summary>
		SqlServer,

		/// <summary>
		/// Use MySQL.
		/// </summary>
		MySql,

		/// <summary>
		/// Use MariaDB.
		/// </summary>
		MariaDB,

		/// <summary>
		/// Use Sqlite.
		/// </summary>
		Sqlite,

		/// <summary>
		/// Use PostgresSql.
		/// </summary>
		PostgresSql,
	}
}
