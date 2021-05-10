using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing <see cref="IConfigurationFile"/>s.
	/// </summary>
	public interface IConfigurationClient
	{
		/// <summary>
		/// List configuration files.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="directory">The path to the directory to list files in.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="ConfigurationFileResponse"/>s in the <paramref name="directory"/>.</returns>
		Task<IReadOnlyList<ConfigurationFileResponse>> List(
			PaginationSettings? paginationSettings,
			string directory,
			CancellationToken cancellationToken);

		/// <summary>
		/// Read a <paramref name="file"/>.
		/// </summary>
		/// <param name="file">The <see cref="IConfigurationFile"/> file to read.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> resulting in a <see cref="Tuple{T1, T2}"/> containing the <see cref="ConfigurationFileResponse"/> and downloaded <see cref="FileTicketResponse"/> <see cref="Stream"/>.</returns>
		Task<Tuple<ConfigurationFileResponse, Stream>> Read(IConfigurationFile file, CancellationToken cancellationToken);

		/// <summary>
		/// Overwrite a <paramref name="file"/>.
		/// </summary>
		/// <param name="file">The <see cref="ConfigurationFileRequest"/>.</param>
		/// <param name="uploadStream">The <see cref="Stream"/> of uploaded data. If <see langword="null"/>, a delete will be attempted.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ConfigurationFileResponse"/>.</returns>
		Task<ConfigurationFileResponse> Write(ConfigurationFileRequest file, Stream uploadStream, CancellationToken cancellationToken);

		/// <summary>
		/// Delete an empty <paramref name="directory"/>.
		/// </summary>
		/// <param name="directory">The <see cref="IConfigurationFile"/> representing the directory to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task DeleteEmptyDirectory(IConfigurationFile directory, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an empty <paramref name="directory"/>.
		/// </summary>
		/// <param name="directory">The <see cref="IConfigurationFile"/> representing the directory to create.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ConfigurationFileResponse"/>.</returns>
		Task<ConfigurationFileResponse> CreateDirectory(IConfigurationFile directory, CancellationToken cancellationToken);
	}
}
