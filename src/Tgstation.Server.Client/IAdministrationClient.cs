using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For managing server administration
	/// </summary>
	public interface IAdministrationClient
	{
		/// <summary>
		/// Get the <see cref="AdministrationResponse"/> represented by the <see cref="IAdministrationClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="AdministrationResponse"/> represented by the <see cref="IAdministrationClient"/></returns>
		Task<AdministrationResponse> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the <see cref="AdministrationResponse"/> setttings
		/// </summary>
		/// <param name="updateRequest">The <see cref="ServerUpdateRequest"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the echoed <paramref name="updateRequest"/>.</returns>
		Task<ServerUpdateRequest> Update(ServerUpdateRequest updateRequest, CancellationToken cancellationToken);

		/// <summary>
		/// Restarts the TGS server
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Restart(CancellationToken cancellationToken);

		/// <summary>
		/// Lists the log files available for download.
		/// </summary>
		/// <param name="paginationSettings">The optional <see cref="PaginationSettings"/> for the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IReadOnlyList{T}"/> of <see cref="LogFileResponse"/> metadata.</returns>
		Task<IReadOnlyList<LogFileResponse>> ListLogs(PaginationSettings? paginationSettings, CancellationToken cancellationToken);

		/// <summary>
		/// Download a given <paramref name="logFile"/>.
		/// </summary>
		/// <param name="logFile">The <see cref="LogFileResponse"/> to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting a <see cref="Tuple{T1, T2}"/> containing the downloaded <see cref="LogFileResponse"/> and associated <see cref="Stream"/>.</returns>
		Task<Tuple<LogFileResponse, Stream>> GetLog(LogFileResponse logFile, CancellationToken cancellationToken);
	}
}
