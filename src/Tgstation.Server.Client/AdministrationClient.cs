using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

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
		public Task<Administration> Read(CancellationToken cancellationToken) => ApiClient.Read<Administration>(Routes.Administration, cancellationToken);

		/// <inheritdoc />
		public Task Update(Administration administration, CancellationToken cancellationToken) => ApiClient.Update(Routes.Administration, administration ?? throw new ArgumentNullException(nameof(administration)), cancellationToken);

		/// <inheritdoc />
		public Task Restart(CancellationToken cancellationToken) => ApiClient.Delete(Routes.Administration, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<LogFile>> ListLogs(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<LogFile>(paginationSettings, Routes.Logs, null, cancellationToken);

		/// <inheritdoc />
		public async Task<Tuple<LogFile, Stream>> GetLog(LogFile logFile, CancellationToken cancellationToken)
		{
			var resultFile = await ApiClient.Read<LogFile>(
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
