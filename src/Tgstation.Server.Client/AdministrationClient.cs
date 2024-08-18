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
	/// <inheritdoc cref="IAdministrationClient" />
	sealed class AdministrationClient : PaginatedClient, IAdministrationClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AdministrationClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		public AdministrationClient(IApiClient apiClient)
			: base(apiClient)
		{
		}

		/// <inheritdoc />
		public ValueTask<AdministrationResponse> Read(bool forceFresh, CancellationToken cancellationToken) => ApiClient.Read<AdministrationResponse>($"{Routes.Administration}?fresh={forceFresh}", cancellationToken);

		/// <inheritdoc />
		public async ValueTask<ServerUpdateResponse> Update(
			ServerUpdateRequest updateRequest,
			Stream? zipFileStream,
			CancellationToken cancellationToken)
		{
			if (updateRequest == null)
				throw new ArgumentNullException(nameof(updateRequest));

			if (updateRequest.UploadZip == true && zipFileStream == null)
				throw new ArgumentNullException(nameof(zipFileStream));

			var result = await ApiClient.Update<ServerUpdateRequest, ServerUpdateResponse>(
				Routes.Administration,
				updateRequest,
				cancellationToken);

			if (updateRequest.UploadZip == true)
				await ApiClient.Upload(result, zipFileStream, cancellationToken).ConfigureAwait(false);

			return result;
		}

		/// <inheritdoc />
		public ValueTask Restart(CancellationToken cancellationToken) => ApiClient.Delete(Routes.Administration, cancellationToken);

		/// <inheritdoc />
		public ValueTask<List<LogFileResponse>> ListLogs(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<LogFileResponse>(paginationSettings, Routes.Logs, null, cancellationToken);

		/// <inheritdoc />
		public async ValueTask<Tuple<LogFileResponse, Stream>> GetLog(LogFileResponse logFile, CancellationToken cancellationToken)
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
