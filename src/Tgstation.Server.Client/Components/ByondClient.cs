using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc cref="Tgstation.Server.Client.Components.IByondClient" />
	sealed class ByondClient : PaginatedClient, IByondClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="ByondClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="ByondClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		public ByondClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task<ByondResponse> ActiveVersion(CancellationToken cancellationToken) => ApiClient.Read<ByondResponse>(Routes.Byond, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<JobResponse> DeleteVersion(ByondVersionDeleteRequest deleteRequest, CancellationToken cancellationToken)
			=> ApiClient.Delete<ByondVersionDeleteRequest, JobResponse>(Routes.Byond, deleteRequest, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<ByondResponse>> InstalledVersions(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<ByondResponse>(paginationSettings, Routes.ListRoute(Routes.Byond), instance.Id, cancellationToken);

		/// <inheritdoc />
		public async Task<ByondInstallResponse> SetActiveVersion(ByondVersionRequest installRequest, Stream? zipFileStream, CancellationToken cancellationToken)
		{
			if (installRequest == null)
				throw new ArgumentNullException(nameof(installRequest));
			if (installRequest.UploadCustomZip == true && zipFileStream == null)
				throw new ArgumentNullException(nameof(zipFileStream));

			var result = await ApiClient.Update<ByondVersionRequest, ByondInstallResponse>(
				Routes.Byond,
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
