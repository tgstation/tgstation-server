using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the <see cref="Byond"/> installation
	/// </summary>
	public interface IByondClient
	{
		/// <summary>
		/// Get the <see cref="Byond"/> active <see cref="System.Version"/> information
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Byond"/> active <see cref="System.Version"/> information</returns>
		Task<Byond> ActiveVersion(CancellationToken cancellationToken);

		/// <summary>
		/// Get all installed <see cref="Byond"/> <see cref="System.Version"/>s
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IReadOnlyList{T}"/> of installed <see cref="Byond"/> <see cref="System.Version"/>s</returns>
		Task<IReadOnlyList<Byond>> InstalledVersions(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Updates the <see cref="Byond"/> information
		/// </summary>
		/// <param name="byond">The <see cref="Byond"/> information to update</param>
		/// <param name="zipFileStream">The <see cref="Stream"/> for the .zip file if <see cref="Byond.UploadCustomZip"/> is <see langword="true"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the updated <see cref="Byond"/> information</returns>
		Task<Byond> SetActiveVersion(Byond byond, Stream zipFileStream, CancellationToken cancellationToken);
	}
}
