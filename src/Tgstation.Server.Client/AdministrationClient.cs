using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class AdministrationClient : PaginatedClient, IAdministrationClient
	{
		/// <summary>
		/// Construct an <see cref="AdministrationClient"/>
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		public AdministrationClient(IApiClient apiClient)
			: base(apiClient)
		{ }

		/// <inheritdoc />
		public Task<AdministrationResponse> Read(CancellationToken cancellationToken) => ApiClient.Read<AdministrationResponse>(Routes.Administration, cancellationToken);

		/// <inheritdoc />
		public Task<ServerUpdateResponse> Update(ServerUpdateRequest updateRequest, CancellationToken cancellationToken) => ApiClient.Update<ServerUpdateRequest, ServerUpdateResponse>(Routes.Administration, updateRequest ?? throw new ArgumentNullException(nameof(updateRequest)), cancellationToken);

		/// <inheritdoc />
		public Task Restart(CancellationToken cancellationToken) => ApiClient.Delete(Routes.Administration, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<LogFileResponse>> ListLogs(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<LogFileResponse>(paginationSettings, Routes.Logs, null, cancellationToken);

		/// <inheritdoc />
		public async Task<Tuple<LogFileResponse, Stream>> GetLog(LogFileResponse logFile, CancellationToken cancellationToken)
		{
			var resultFile = await ApiClient.Read<LogFileResponse>(
				Routes.Logs + Routes.SanitizeGetPath(
					HttpUtility.UrlEncode(
						logFile?.Name ?? throw new ArgumentNullException(nameof(logFile)))),
				cancellationToken)
				.ConfigureAwait(false);

			var stream = await ApiClient.Download(resultFile, cancellationToken).ConfigureAwait(false);
			try
			{
				return Tuple.Create(resultFile, stream);
			}
			catch
			{
				stream.Dispose();
				throw;
			}
		}
	}
}
