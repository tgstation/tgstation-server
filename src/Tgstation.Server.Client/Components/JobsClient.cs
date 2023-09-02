using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc cref="IJobsClient" />
	sealed class JobsClient : PaginatedClient, IJobsClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="JobsClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="JobsClient"/> class.
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		public JobsClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task Cancel(JobResponse job, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.Jobs, job?.Id ?? throw new ArgumentNullException(nameof(job))), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<JobResponse>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<JobResponse>(paginationSettings, Routes.ListRoute(Routes.Jobs), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<JobResponse>> ListActive(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<JobResponse>(paginationSettings, Routes.Jobs, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<JobResponse> GetId(EntityId job, CancellationToken cancellationToken) => ApiClient.Read<JobResponse>(Routes.SetID(Routes.Jobs, job?.Id ?? throw new ArgumentNullException(nameof(job))), instance.Id!.Value, cancellationToken);
	}
}
