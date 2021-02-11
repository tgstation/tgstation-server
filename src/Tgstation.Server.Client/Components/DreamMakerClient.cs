using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class DreamMakerClient : PaginatedClient, IDreamMakerClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="DreamMakerClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="DreamMakerClient"/>
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="Instance"/></param>
		public DreamMakerClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task<JobResponse> Compile(CancellationToken cancellationToken) => ApiClient.Create<JobResponse>(Routes.DreamMaker, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<CompileJobResponse> GetCompileJob(EntityId compileJob, CancellationToken cancellationToken) => ApiClient.Read<CompileJobResponse>(Routes.SetID(Routes.DreamMaker, compileJob?.Id ?? throw new ArgumentNullException(nameof(compileJob))), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<CompileJobResponse>> ListCompileJobs(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<CompileJobResponse>(paginationSettings, Routes.ListRoute(Routes.DreamMaker), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<DreamMakerResponse> Read(CancellationToken cancellationToken) => ApiClient.Read<DreamMakerResponse>(Routes.DreamMaker, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<DreamMakerResponse> Update(DreamMakerRequest dreamMaker, CancellationToken cancellationToken) => ApiClient.Update<DreamMakerRequest, DreamMakerResponse>(Routes.DreamMaker, dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker)), instance.Id!.Value, cancellationToken);
	}
}
