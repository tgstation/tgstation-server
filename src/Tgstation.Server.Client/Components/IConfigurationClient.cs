using System.Collections.Generic;
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
		/// <param name="directory">The path to the directory to list files in</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="IReadOnlyList{T}"/> of <see cref="ConfigurationFile"/>s in the <paramref name="directory"/></returns>
		Task<IReadOnlyList<ConfigurationFile>> List(string directory, CancellationToken cancellationToken);

		/// <summary>
		/// Read a <see cref="ConfigurationFile"/> file
		/// </summary>
		/// <param name="file">The <see cref="ConfigurationFile"/> file to read</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task<ConfigurationFile> Read(ConfigurationFile file, CancellationToken cancellationToken);

		/// <summary>
		/// Overwrite a <see cref="ConfigurationFile"/> file
		/// </summary>
		/// <param name="file">The <see cref="ConfigurationFile"/> file to write</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the new <see cref="ConfigurationFile"/></returns>
		Task<ConfigurationFile> Write(ConfigurationFile file, CancellationToken cancellationToken);

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
