using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the <see cref="ByondInstallResponse"/> installation.
	/// </summary>
	public interface IByondClient
	{
		/// <summary>
		/// Get the <see cref="ByondInstallResponse"/> active <see cref="System.Version"/> information.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="ByondInstallResponse"/> active <see cref="System.Version"/> information.</returns>
		ValueTask<ByondResponse> ActiveVersion(CancellationToken cancellationToken);

		/// <summary>
		/// Get all installed <see cref="ByondInstallResponse"/> <see cref="System.Version"/>s.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an <see cref="List{T}"/> of installed <see cref="ByondInstallResponse"/> <see cref="System.Version"/>s.</returns>
		ValueTask<List<ByondResponse>> InstalledVersions(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Updates the active BYOND version.
		/// </summary>
		/// <param name="installRequest">The <see cref="ByondVersionRequest"/>.</param>
		/// <param name="zipFileStream">The <see cref="Stream"/> for the .zip file if <see cref="ByondVersionRequest.UploadCustomZip"/> is <see langword="true"/>. Will be ignored if it is <see langword="false"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the updated <see cref="ByondInstallResponse"/> information.</returns>
		ValueTask<ByondInstallResponse> SetActiveVersion(ByondVersionRequest installRequest, Stream? zipFileStream, CancellationToken cancellationToken);

		/// <summary>
		/// Starts a jobs to delete a specific BYOND version.
		/// </summary>
		/// <param name="deleteRequest">The <see cref="ByondVersionDeleteRequest"/> specifying the version to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="JobResponse"/> for the delete job.</returns>
		ValueTask<JobResponse> DeleteVersion(ByondVersionDeleteRequest deleteRequest, CancellationToken cancellationToken);
	}
}
