namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Response when reading configuration files.
	/// </summary>
	public sealed class ConfigurationFileResponse : FileTicketResponse, IConfigurationFile
	{
		/// <inheritdoc />
		public string? Path { get; set; }

		/// <inheritdoc />
		public string? LastReadHash { get; set; }

		/// <summary>
		/// If <see cref="Path"/> represents a directory
		/// </summary>
		public bool? IsDirectory { get; set; }

		/// <summary>
		/// If access to the <see cref="IConfigurationFile"/> file was denied for the operation
		/// </summary>
		[ResponseOptions]
		public bool? AccessDenied { get; set; }
	}
}
