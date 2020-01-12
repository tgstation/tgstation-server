using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class JobsClient : IJobsClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="JobsClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="JobsClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="JobsClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="Instance"/></param>
		public JobsClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task Cancel(Job job, CancellationToken cancellationToken) => apiClient.Delete(Routes.SetID(Routes.Jobs, job?.Id ?? throw new ArgumentNullException(nameof(job))), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<EntityId>> List(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<EntityId>>(Routes.ListRoute(Routes.Jobs), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<Job>> ListActive(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<Job>>(Routes.Jobs, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<Job> GetId(EntityId job, CancellationToken cancellationToken) => apiClient.Read<Job>(Routes.SetID(Routes.Jobs, job?.Id ?? throw new ArgumentNullException(nameof(job))), instance.Id, cancellationToken);
	}
}