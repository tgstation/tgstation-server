namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Type of database to user
	/// </summary>
	enum DatabaseType
	{
		/// <summary>
		/// Use Microsoft SQL Server
		/// </summary>
		SqlServer,
		/// <summary>
		/// Use MySQL/MariaDB
		/// </summary>
		MySql,
		/// <summary>
		/// Use SQLite 3
		/// </summary>
		Sqlite
	}
}
