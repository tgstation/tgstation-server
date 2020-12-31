using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components.StaticFiles
{
	/// <summary>
	/// For managing the Configuration directory
	/// </summary>
	public interface IConfiguration : IHostedService, IEventConsumer, IDisposable
	{
		/// <summary>
		/// Copies all files in the CodeModifications directory to <paramref name="destination"/>
		/// </summary>
		/// <param name="dmeFile">The .dme file being compiled</param>
		/// <param name="destination">Path to the destination folder</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ServerSideModifications"/> if any</returns>
		Task<ServerSideModifications> CopyDMFilesTo(string dmeFile, string destination, CancellationToken cancellationToken);

		/// <summary>
		/// Symlinks all directories in the GameData directory to <paramref name="destination"/>
		/// </summary>
		/// <param name="destination">Path to the destination folder</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task SymlinkStaticFilesTo(string destination, CancellationToken cancellationToken);

		/// <summary>
		/// Get <see cref="ConfigurationFile"/> for all items in a given <paramref name="configurationRelativePath"/>
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ConfigurationFile"/>s for the items in the directory. <see cref="FileTicketResult.FileTicket"/> and <see cref="ConfigurationFile.LastReadHash"/> will both be <see langword="null"/></returns>
		Task<IReadOnlyList<ConfigurationFile>> ListDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Reads a given <paramref name="configurationRelativePath"/>
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ConfigurationFile"/> of the file</returns>
		Task<ConfigurationFile> Read(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Create an empty directory at <paramref name="configurationRelativePath"/>
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation. Usage may result in partial writes</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="true"/> if the directory already existed, <see langword="false"/> otherwise</returns>
		Task<bool> CreateDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to delete an empty directory at <paramref name="configurationRelativePath"/>
		/// </summary>
		/// <param name="configurationRelativePath">The path of the empty directory to delete</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/></param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns><see langword="true"/> if the directory was empty and deleted, <see langword="false"/> otherwise</returns>
		Task<bool> DeleteDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Writes to a given <paramref name="configurationRelativePath"/>
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/></param>
		/// <param name="previousHash">The hash any existing file must match in order for the write to succeed</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation. Usage may result in partial writes</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="ConfigurationFile"/> or <see langword="null"/> if the write failed due to <see cref="ConfigurationFile.LastReadHash"/> conflicts</returns>
		Task<ConfigurationFile> Write(string configurationRelativePath, ISystemIdentity systemIdentity, string previousHash, CancellationToken cancellationToken);
	}
}
