using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class AdministrationClient : IAdministrationClient
	{
		/// <summary>
		/// The <see cref="apiClient"/> for the <see cref="AdministrationClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// Construct an <see cref="AdministrationClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		public AdministrationClient(IApiClient apiClient)
		{
			this.apiClient = apiClient;
		}

		/// <inheritdoc />
		public Task<Administration> Read(CancellationToken cancellationToken) => apiClient.Read<Administration>(Routes.Administration, cancellationToken);

		/// <inheritdoc />
		public Task Update(Administration administration, CancellationToken cancellationToken) => apiClient.Update(Routes.Administration, administration ?? throw new ArgumentNullException(nameof(administration)), cancellationToken);

		/// <inheritdoc />
		public Task Restart(CancellationToken cancellationToken) => apiClient.Delete(Routes.Administration, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<LogFile>> ListLogs(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<LogFile>>(Routes.Logs, cancellationToken);

		/// <inheritdoc />
		public Task<LogFile> GetLog(LogFile logFile, CancellationToken cancellationToken) => apiClient.Read<LogFile>(
			Routes.Logs + Routes.SanitizeGetPath(
				HttpUtility.UrlEncode(
					logFile?.Name ?? throw new ArgumentNullException(nameof(logFile)))),
			cancellationToken);
	}
}
