using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For managing the Configuration directory
	/// </summary>
	interface IConfiguration
	{
		/// <summary>
		/// Copies all files in the CodeModifications directory to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Path to the destination folder</param>
		/// <returns>A <see cref="Task{TResult}"/> resultin in a <see cref="IReadOnlyList{T}"/> of #include lines for the .dm files copied</returns>
		Task<IReadOnlyList<string>> CopyDMFilesTo(string destination, CancellationToken cancellationToken);

		/// <summary>
		/// Symlinks all directories in the GameData directory to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Path to the destination folder</param>
		/// <param name="cancellationToken"></param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SymlinkStaticFilesTo(string destination, CancellationToken cancellationToken);

		/// <summary>
		/// Get <see cref="ConfigurationFileMetadata"/> for all items in a given <paramref name="configurationRelativePath"/>
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ConfigurationFileMetadata"/> for the items in the directory</returns>
		Task<IReadOnlyList<ConfigurationFileMetadata>> ListDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Reads a given <paramref name="configurationRelativePath"/>
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Api.Models.Configuration"/> of the file</returns>
		Task<Api.Models.Configuration> Read(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Writes to a given <paramref name="configurationRelativePath"/>
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory</param>
		/// <param name="data">The data to write. If <see langword="null"/>, the file is deleted</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation. Usage may result in partial writes</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the operation succeeded, <see langword="false"/> if it failed due to permission errors</returns>
		Task<bool> Write(string configurationRelativePath, ISystemIdentity systemIdentity, byte[] data, CancellationToken cancellationToken);
	}
}