using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Components.Events;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Components.StaticFiles
{
	/// <summary>
	/// For managing the Configuration directory.
	/// </summary>
	public interface IConfiguration : IComponentService, IEventConsumer, IDisposable
	{
		/// <summary>
		/// Copies all files in the CodeModifications directory to <paramref name="destination"/>.
		/// </summary>
		/// <param name="dmeFile">The .dme file being compiled.</param>
		/// <param name="destination">Path to the destination folder.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="ServerSideModifications"/> if any.</returns>
		ValueTask<ServerSideModifications> CopyDMFilesTo(string dmeFile, string destination, CancellationToken cancellationToken);

		/// <summary>
		/// Symlinks all directories in the GameData directory to <paramref name="destination"/>.
		/// </summary>
		/// <param name="destination">Path to the destination folder.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask SymlinkStaticFilesTo(string destination, CancellationToken cancellationToken);

		/// <summary>
		/// Get <see cref="ConfigurationFileResponse"/>s for all items in a given <paramref name="configurationRelativePath"/>.
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory.</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an <see cref="IOrderedQueryable{T}"/> of the <see cref="ConfigurationFileResponse"/>s for the items in the directory. <see cref="FileTicketResponse.FileTicket"/> and <see cref="IConfigurationFile.LastReadHash"/> will both be <see langword="null"/>. <see langword="null"/> will be returned if the operation failed due to access contention.</returns>
		ValueTask<IOrderedQueryable<ConfigurationFileResponse>> ListDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Reads a given <paramref name="configurationRelativePath"/>.
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory.</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="ConfigurationFileResponse"/> of the file. <see langword="null"/> will be returned if the operation failed due to access contention.</returns>
		ValueTask<ConfigurationFileResponse> Read(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Create an empty directory at <paramref name="configurationRelativePath"/>.
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory.</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation. Usage may result in partial writes.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the directory already existed, <see langword="false"/> otherwise. <see langword="null"/> will be returned if the operation failed due to access contention.</returns>
		ValueTask<bool?> CreateDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Attempt to delete an empty directory at <paramref name="configurationRelativePath"/>.
		/// </summary>
		/// <param name="configurationRelativePath">The path of the empty directory to delete.</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="true"/> if the directory was empty and deleted, <see langword="false"/> otherwise. <see langword="null"/> will be returned if the operation failed due to access contention.</returns>
		ValueTask<bool?> DeleteDirectory(string configurationRelativePath, ISystemIdentity systemIdentity, CancellationToken cancellationToken);

		/// <summary>
		/// Writes to a given <paramref name="configurationRelativePath"/>.
		/// </summary>
		/// <param name="configurationRelativePath">The relative path in the Configuration directory.</param>
		/// <param name="systemIdentity">The <see cref="ISystemIdentity"/> for the operation. If <see langword="null"/>, the operation will be performed as the user of the <see cref="Core.Application"/>.</param>
		/// <param name="previousHash">The hash any existing file must match in order for the write to succeed.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation. Usage may result in partial writes.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the updated <see cref="ConfigurationFileResponse"/> and associated writing <see cref="FileTicketResponse"/>. <see langword="null"/> will be returned if the operation failed due to access contention.</returns>
		ValueTask<ConfigurationFileResponse> Write(string configurationRelativePath, ISystemIdentity systemIdentity, string previousHash, CancellationToken cancellationToken);
	}
}
