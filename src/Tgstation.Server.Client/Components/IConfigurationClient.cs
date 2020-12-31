using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing <see cref="ConfigurationFile"/> files
	/// </summary>
	public interface IConfigurationClient
	{
		/// <summary>
		/// List configuration files
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="directory">The path to the directory to list files in</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="ConfigurationFile"/>s in the <paramref name="directory"/></returns>
		Task<IReadOnlyList<ConfigurationFile>> List(
			PaginationSettings? paginationSettings,
			string directory,
			CancellationToken cancellationToken);

		/// <summary>
		/// Read a <see cref="ConfigurationFile"/> file
		/// </summary>
		/// <param name="file">The <see cref="ConfigurationFile"/> file to read</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> resulting in a <see cref="Tuple{T1, T2}"/> containing the <see cref="ConfigurationFile"/> and downloaded <see cref="FileTicketResult"/> <see cref="Stream"/>.</returns>
		Task<Tuple<ConfigurationFile, Stream>> Read(ConfigurationFile file, CancellationToken cancellationToken);

		/// <summary>
		/// Overwrite a <see cref="ConfigurationFile"/> file
		/// </summary>
		/// <param name="file">The <see cref="ConfigurationFile"/> file to write</param>
		/// <param name="uploadStream">The <see cref="Stream"/> of uploaded data. If <see langword="null"/>, a delete will be attempted.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ConfigurationFile"/>.</returns>
		Task<ConfigurationFile> Write(ConfigurationFile file, Stream uploadStream, CancellationToken cancellationToken);

		/// <summary>
		/// Delete an empty <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The <see cref="ConfigurationFile"/> representing the directory to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task DeleteEmptyDirectory(ConfigurationFile directory, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an empty <paramref name="directory"/>
		/// </summary>
		/// <param name="directory">The <see cref="ConfigurationFile"/> representing the directory to create</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ConfigurationFile"/></returns>
		Task<ConfigurationFile> CreateDirectory(ConfigurationFile directory, CancellationToken cancellationToken);
	}
}
