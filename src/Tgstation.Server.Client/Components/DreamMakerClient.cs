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
		/// The <see cref="Instance"/> for the <see cref="DreamMakerClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="DreamMakerClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		public DreamMakerClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public ValueTask<JobResponse> Compile(CancellationToken cancellationToken) => ApiClient.Create<JobResponse>(Routes.DreamMaker, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<CompileJobResponse> GetCompileJob(EntityId compileJob, CancellationToken cancellationToken) => ApiClient.Read<CompileJobResponse>(Routes.SetID(Routes.DreamMaker, compileJob?.Id ?? throw new ArgumentNullException(nameof(compileJob))), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<List<CompileJobResponse>> ListCompileJobs(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<CompileJobResponse>(paginationSettings, Routes.ListRoute(Routes.DreamMaker), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<DreamMakerResponse> Read(CancellationToken cancellationToken) => ApiClient.Read<DreamMakerResponse>(Routes.DreamMaker, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<DreamMakerResponse> Update(DreamMakerRequest dreamMaker, CancellationToken cancellationToken) => ApiClient.Update<DreamMakerRequest, DreamMakerResponse>(Routes.DreamMaker, dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker)), instance.Id!.Value, cancellationToken);
	}
}
