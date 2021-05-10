namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// Represents a request to update a configuration file.
	/// </summary>
	public sealed class ConfigurationFileRequest : IConfigurationFile
	{
		/// <inheritdoc />
		[RequestOptions(FieldPresence.Required)]
		public string? Path { get; set; }

		/// <inheritdoc />
		public string? LastReadHash { get; set; }
	}
}
