using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a game configuration file. Create and delete actions uncerimonuously overwrite/delete files
	/// </summary>
	public sealed class ConfigurationFile
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

		/// <summary>
		/// The content of the <see cref="ConfigurationFile"/>. Will be <see langword="null"/> if <see cref="AccessDenied"/> is <see langword="true"/> or during listing and write operations
		/// </summary>
#pragma warning disable CA1819, SA1011 // Properties should not return arrays, Closing square bracket should be followed by a space
		public byte[]? Content { get; set; }
#pragma warning restore CA1819, SA1011 // Properties should not return arrays, Closing square bracket should be followed by a space
	}
}
