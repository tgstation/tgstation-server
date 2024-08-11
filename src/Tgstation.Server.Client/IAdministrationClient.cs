using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For managing server administration.
	/// </summary>
	public interface IAdministrationClient
	{
		/// <summary>
		/// Get the <see cref="AdministrationResponse"/> represented by the <see cref="IAdministrationClient"/>.
		/// </summary>
		/// <param name="forceFresh">If <see langword="true"/> the response will be forcefully regenerated.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="AdministrationResponse"/> represented by the <see cref="IAdministrationClient"/>.</returns>
		ValueTask<AdministrationResponse> Read(bool forceFresh = false, CancellationToken cancellationToken = default);

		/// <summary>
		/// Updates the <see cref="AdministrationResponse"/> setttings.
		/// </summary>
		/// <param name="updateRequest">The <see cref="ServerUpdateRequest"/>.</param>
		/// <param name="zipFileStream">The <see cref="Stream"/> for the .zip file if <see cref="ServerUpdateRequest.UploadZip"/> is <see langword="true"/>. Will be ignored if it is <see langword="false"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the echoed <see cref="ServerUpdateResponse"/>.</returns>
		ValueTask<ServerUpdateResponse> Update(ServerUpdateRequest updateRequest, Stream? zipFileStream, CancellationToken cancellationToken);

		/// <summary>
		/// Restarts the TGS server.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask Restart(CancellationToken cancellationToken);

		/// <summary>
		/// Lists the log files available for download.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an <see cref="List{T}"/> of <see cref="LogFileResponse"/> metadata.</returns>
		ValueTask<List<LogFileResponse>> ListLogs(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Download a given <paramref name="logFile"/>.
		/// </summary>
		/// <param name="logFile">The <see cref="LogFileResponse"/> to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting a <see cref="Tuple{T1, T2}"/> containing the downloaded <see cref="LogFileResponse"/> and associated <see cref="Stream"/>.</returns>
		ValueTask<Tuple<LogFileResponse, Stream>> GetLog(LogFileResponse logFile, CancellationToken cancellationToken);
	}
}
