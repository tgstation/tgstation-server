using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a game configuration file. Create and delete actions uncerimonuously overwrite/delete files
	/// </summary>
	[Model(RightsType.Configuration, CanCrud = true, CanList = true, RequiresInstance = true, ReadRight = ConfigurationRights.Read, WriteRight = ConfigurationRights.Write)]
	public sealed class ConfigurationFile
	{
		/// <summary>
		/// The path to the <see cref="ConfigurationFile"/> file
		/// </summary>
		[Permissions(DenyWrite = true)]
		public string Path { get; set; }

		/// <summary>
		/// If access to the <see cref="ConfigurationFile"/> file was denied for the operation
		/// </summary>
		[Permissions(DenyWrite = true)]
		public bool? AccessDenied { get; set; }

		/// <summary>
		/// If <see cref="Path"/> represents a directory
		/// </summary>
		[Permissions(DenyWrite = true)]
		public bool? IsDirectory { get; set; }

		/// <summary>
		/// The MD5 hash of the file when last read by the user. If this doesn't match during update actions, the write will be denied with <see cref="System.Net.HttpStatusCode.Conflict"/>
		/// </summary>
		[Permissions(DenyWrite = true)]
		public string LastReadHash { get; set; }

		/// <summary>
		/// The content of the <see cref="ConfigurationFile"/>. Will be <see langword="null"/> if <see cref="AccessDenied"/> is <see langword="true"/> or during listing and write operations
		/// </summary>
		public byte[] Content { get; set; }
	}
}
