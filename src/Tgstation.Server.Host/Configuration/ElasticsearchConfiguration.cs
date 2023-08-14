using System;

namespace Tgstation.Server.Host.Configuration
	{
	/// <summary>
	/// Configuration options pertaining to elasticsearch log storage.
	/// </summary>
	public sealed class ElasticsearchConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="ElasticsearchConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Elasticsearch";

		/// <summary>
		/// Do we want to enable elasticsearch or not?.
		/// </summary>
		public bool Enable { get; set; }

		/// <summary>
		/// The host of the elasticsearch endpoint.
		/// </summary>
		public Uri Host { get; set; }

		/// <summary>
		/// Username for elasticsearch.
		/// </summary>
		public string Username { get; set; }

		/// <summary>
		/// Password for elasticsearch.
		/// </summary>
		public string Password { get; set; }
	}
}
