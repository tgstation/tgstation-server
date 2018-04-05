using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing <see cref="Configuration"/> files
	/// </summary>
	public interface IConfigurationClient : IRightsClient<ConfigurationRights>
	{
		/// <summary>
		/// List configuration files
		/// </summary>
		/// <param name="directory">The path to the directory to list files in</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="Configuration"/>s in the <paramref name="directory"/></returns>
		Task<IReadOnlyList<Configuration>> List(string directory, CancellationToken cancellationToken);

		/// <summary>
		/// Read a <see cref="Configuration"/> file
		/// </summary>
		/// <param name="file">The <see cref="Configuration"/> file to read</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Read(Configuration file, CancellationToken cancellationToken);

		/// <summary>
		/// Overwrite a <see cref="Configuration"/> file with integrity checks
		/// </summary>
		/// <param name="file">The <see cref="Configuration"/> file to write</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Write(Configuration file, CancellationToken cancellationToken);

		/// <summary>
		/// Create/overwrite a <see cref="Configuration"/> file
		/// </summary>
		/// <param name="file">The <see cref="Configuration"/> file to write</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Create(Configuration file, CancellationToken cancellationToken);

		/// <summary>
		/// Delete a <see cref="Configuration"/> file
		/// </summary>
		/// <param name="file">The <see cref="Configuration"/> file to delete</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Delete(Configuration file, CancellationToken cancellationToken);
	}
}
