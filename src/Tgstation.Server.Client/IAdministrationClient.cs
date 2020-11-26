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
		/// Get the <see cref="Administration"/> represented by the <see cref="IAdministrationClient"/>
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Administration"/> represented by the <see cref="IAdministrationClient"/></returns>
		Task<Administration> Read(CancellationToken cancellationToken);

		/// <summary>
		/// Updates the <see cref="Administration"/> setttings
		/// </summary>
		/// <param name="administration">The <see cref="Administration"/> to update</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Update(Administration administration, CancellationToken cancellationToken);

		/// <summary>
		/// Restarts the TGS server
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		Task Restart(CancellationToken cancellationToken);

		/// <summary>
		/// Lists the log files available for download.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an <see cref="IReadOnlyList{T}"/> of <see cref="LogFile"/> metadata.</returns>
		Task<IReadOnlyList<LogFile>> ListLogs(CancellationToken cancellationToken);

		/// <summary>
		/// Download a given <paramref name="logFile"/>.
		/// </summary>
		/// <param name="logFile">The <see cref="LogFile"/> to download.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting a <see cref="Tuple{T1, T2}"/> containing the downloaded <see cref="LogFile"/> and associated <see cref="Stream"/>.</returns>
		Task<Tuple<LogFile, Stream>> GetLog(LogFile logFile, CancellationToken cancellationToken);
	}
}
