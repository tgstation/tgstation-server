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
	/// <inheritdoc cref="IEngineClient" />
	sealed class EngineClient : PaginatedClient, IEngineClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="EngineClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="EngineClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		public EngineClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public ValueTask<EngineResponse> ActiveVersion(CancellationToken cancellationToken) => ApiClient.Read<EngineResponse>(Routes.Engine, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<JobResponse> DeleteVersion(EngineVersionDeleteRequest deleteRequest, CancellationToken cancellationToken)
			=> ApiClient.Delete<EngineVersionDeleteRequest, JobResponse>(Routes.Engine, deleteRequest, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<List<EngineResponse>> InstalledVersions(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<EngineResponse>(paginationSettings, Routes.ListRoute(Routes.Engine), instance.Id, cancellationToken);

		/// <inheritdoc />
		public async ValueTask<EngineInstallResponse> SetActiveVersion(EngineVersionRequest installRequest, Stream? zipFileStream, CancellationToken cancellationToken)
		{
			if (installRequest == null)
				throw new ArgumentNullException(nameof(installRequest));
			if (installRequest.UploadCustomZip == true && zipFileStream == null)
				throw new ArgumentNullException(nameof(zipFileStream));

			var result = await ApiClient.Update<EngineVersionRequest, EngineInstallResponse>(
				Routes.Engine,
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
