using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a game configuration file. Create and delete actions uncerimonuously overwrite/delete files
	/// </summary>
	public sealed class ConfigurationFile : FileTicketResult
	{
		/// <summary>
		/// The path to the <see cref="ConfigurationFile"/> file
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		public string? Path { get; set; }

		/// <summary>
		/// If access to the <see cref="ConfigurationFile"/> file was denied for the operation
		/// </summary>
		public bool? AccessDenied { get; set; }

		/// <summary>
		/// If <see cref="Path"/> represents a directory
		/// </summary>
		public bool? IsDirectory { get; set; }

		/// <summary>
		/// The MD5 hash of the file when last read by the user. If this doesn't match during update actions, the write will be denied with <see cref="System.Net.HttpStatusCode.Conflict"/>
		/// </summary>
		public string? LastReadHash { get; set; }
	}
}
