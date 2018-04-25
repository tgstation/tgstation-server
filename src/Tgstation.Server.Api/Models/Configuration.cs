using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a static game file. Create and delete actions uncerimonuously overwrite/delete files
	/// </summary>
#pragma warning disable CA1724 // System.Configuration name conflict
	public sealed class Configuration : ConfigurationFileMetadata
#pragma warning restore CA1724 // System.Configuration name conflict
	{
		/// <summary>
		/// The content of the <see cref="Configuration"/> file. Will be <see langword="null"/> if <see cref="ConfigurationFileMetadata.ReadDenied"/> is <see langword="true"/> or during listing operations
		/// </summary>
		public byte[] Content { get; set; }
	}
}
