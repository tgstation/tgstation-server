using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <summary>
	/// For managing the engine installations.
	/// </summary>
	public interface IEngineClient
	{
		/// <summary>
		/// Get the <see cref="EngineInstallResponse"/> active <see cref="Api.Models.Internal.EngineVersion"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="EngineResponse"/>.</returns>
		ValueTask<EngineResponse> ActiveVersion(CancellationToken cancellationToken);

		/// <summary>
		/// Get all installed <see cref="EngineInstallResponse"/> <see cref="System.Version"/>s.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an <see cref="List{T}"/> of installed <see cref="EngineResponse"/>s.</returns>
		ValueTask<List<EngineResponse>> InstalledVersions(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Updates the active engine version.
		/// </summary>
		/// <param name="installRequest">The <see cref="EngineVersionRequest"/>.</param>
		/// <param name="zipFileStream">The <see cref="Stream"/> for the .zip file if <see cref="EngineVersionRequest.UploadCustomZip"/> is <see langword="true"/>. Will be ignored if it is <see langword="false"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="EngineInstallResponse"/>.</returns>
		ValueTask<EngineInstallResponse> SetActiveVersion(EngineVersionRequest installRequest, Stream? zipFileStream, CancellationToken cancellationToken);

		/// <summary>
		/// Starts a job to delete a specific engine version.
		/// </summary>
		/// <param name="deleteRequest">The <see cref="EngineVersionDeleteRequest"/> specifying the <see cref="Api.Models.Internal.EngineVersion"/> to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="JobResponse"/> for the delete job.</returns>
		ValueTask<JobResponse> DeleteVersion(EngineVersionDeleteRequest deleteRequest, CancellationToken cancellationToken);
	}
}
