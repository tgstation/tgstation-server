namespace Tgstation.Server.Host.Configuration
{
	sealed class DatabaseConfiguration
	{
		public const string Section = "Database";
		public DatabaseType DatabaseType { get; set; }
		public string ConnectionString { get; set; }
	}
}
