using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Client;
using Tgstation.Server.Host.Controllers.Legacy.Models;

namespace Tgstation.Server.Tests.Live.Instance
{
	/// <inheritdoc cref="IByondClient" />
	sealed class LegacyByondClient : PaginatedClient
	{
		const string Route = Routes.Root + "Byond";

		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="ByondClient"/>.
		/// </summary>
		readonly Api.Models.Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		public LegacyByondClient(IApiClient apiClient, Api.Models.Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public ValueTask<ByondResponse> ActiveVersion(CancellationToken cancellationToken) => ApiClient.Read<ByondResponse>(Route, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<JobResponse> DeleteVersion(ByondVersionDeleteRequest deleteRequest, CancellationToken cancellationToken)
			=> ApiClient.Delete<ByondVersionDeleteRequest, JobResponse>(Route, deleteRequest, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<List<ByondResponse>> InstalledVersions(PaginationSettings paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<ByondResponse>(paginationSettings, Routes.ListRoute(Route), instance.Id, cancellationToken);

		/// <inheritdoc />
		public async ValueTask<ByondInstallResponse> SetActiveVersion(ByondVersionRequest installRequest, Stream zipFileStream, CancellationToken cancellationToken)
		{
			if (installRequest == null)
				throw new ArgumentNullException(nameof(installRequest));
			if (installRequest.UploadCustomZip == true && zipFileStream == null)
				throw new ArgumentNullException(nameof(zipFileStream));

			var result = await ApiClient.Update<ByondVersionRequest, ByondInstallResponse>(
				Route,
				installRequest,
				instance.Id!.Value,
				cancellationToken)
				.ConfigureAwait(false);

			if (installRequest.UploadCustomZip == true)
				await ApiClient.Upload(result, zipFileStream, cancellationToken).ConfigureAwait(false);

			return result;
		}
	}
}
