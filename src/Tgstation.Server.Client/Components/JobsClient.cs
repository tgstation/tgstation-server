using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class JobsClient : PaginatedClient, IJobsClient
	{
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="JobsClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="JobsClient"/>
		/// </summary>
		/// <param name="apiClient">The <see cref="IApiClient"/> for the <see cref="PaginatedClient"/>.</param>
		/// <param name="instance">The value of <see cref="Instance"/></param>
		public JobsClient(IApiClient apiClient, Instance instance)
			: base(apiClient)
		{
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task Cancel(Job job, CancellationToken cancellationToken) => ApiClient.Delete(Routes.SetID(Routes.Jobs, job?.Id ?? throw new ArgumentNullException(nameof(job))), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<Job>> List(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<Job>(paginationSettings, Routes.ListRoute(Routes.Jobs), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<Job>> ListActive(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<Job>(paginationSettings, Routes.Jobs, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<Job> GetId(EntityId job, CancellationToken cancellationToken) => ApiClient.Read<Job>(Routes.SetID(Routes.Jobs, job?.Id ?? throw new ArgumentNullException(nameof(job))), instance.Id, cancellationToken);
	}
}
