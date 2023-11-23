using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

#nullable disable

namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration options for the <see cref="Database.DatabaseContext"/>.
	/// </summary>
	public sealed class DatabaseConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="DatabaseConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Database";

		/// <summary>
		/// The <see cref="Configuration.DatabaseType"/> to create.
		/// </summary>
		[JsonConverter(typeof(StringEnumConverter))]
		public DatabaseType DatabaseType { get; set; }

		/// <summary>
		/// If the admin user should be enabled and have it's password reset.
		/// </summary>
		public bool ResetAdminPassword { get; set; }

		/// <summary>
		/// The connection string for the database.
		/// </summary>
		public string ConnectionString { get; set; }

		/// <summary>
		/// If the database should be deleted on application startup. Should not be used in production!.
		/// </summary>
		public bool DropDatabase { get; set; }

		/// <summary>
		/// The <see cref="string"/> form of the <see cref="global::System.Version"/> of the target server.
		/// </summary>
		public string ServerVersion { get; set; }
	}
}
