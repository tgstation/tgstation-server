namespace Tgstation.Server.Host.Configuration
	{
	/// <summary>
	/// Configuration options pertaining to elasticsearch log storage.
	/// </summary>
	sealed class ElasticsearchConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="ElasticsearchConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Elasticsearch";

		/// <summary>
		/// Default value of <see cref="Enable"/>.
		/// </summary>
		const bool DefaultEnable = false;

		/// <summary>
		/// Default value of <see cref="Host"/>.
		/// </summary>
		const string DefaultHost = "http://127.0.0.1:9200"; // localhost

		/// <summary>
		/// Default value of <see cref="Username"/>.
		/// </summary>
		const string DefaultUsername = "my_username";

		/// <summary>
		/// Default value of <see cref="Password"/>.
		/// </summary>
		const string DefaultPassword = "my_password";

		/// <summary>
		/// Do we want to enable elasticsearch or not?.
		/// </summary>
		public bool Enable { get; set; } = DefaultEnable;

		/// <summary>
		/// The host of the elasticsearch endpoint.
		/// </summary>
		public string Host { get; set; } = DefaultHost;

		/// <summary>
		/// Username for elasticsearch.
		/// </summary>
		public string Username { get; set; } = DefaultUsername;

		/// <summary>
		/// Password for elasticsearch.
		/// </summary>
		public string Password { get; set; } = DefaultPassword;
	}
}
