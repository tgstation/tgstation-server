using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a game configuration file. Create and delete actions uncerimonuously overwrite/delete files.
	/// </summary>
	public interface IConfigurationFile
	{
		/// <summary>
		/// The path to the <see cref="IConfigurationFile"/> file.
		/// </summary>
		[StringLength(Limits.MaximumStringLength)]
		public string? Path { get; set; }

		/// <summary>
		/// The MD5 hash of the file when last read by the user. If this doesn't match during update actions, the write will be denied with <see cref="System.Net.HttpStatusCode.Conflict"/>.
		/// </summary>
		[ResponseOptions]
		public string? LastReadHash { get; set; }
	}
}
