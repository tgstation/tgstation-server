using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class DreamMakerClient : IDreamMakerClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="DreamMakerClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="DreamMakerClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="DreamMakerClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="Instance"/></param>
		public DreamMakerClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task<Job> Compile(CancellationToken cancellationToken) => apiClient.Create<Job>(Routes.DreamMaker, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<CompileJob> GetCompileJob(EntityId compileJob, CancellationToken cancellationToken) => apiClient.Read<CompileJob>(Routes.SetID(Routes.DreamMaker, compileJob?.Id ?? throw new ArgumentNullException(nameof(compileJob))), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<EntityId>> GetJobIds(CancellationToken cancellationToken) => apiClient.Read<IReadOnlyList<EntityId>>(Routes.ListRoute(Routes.DreamMaker), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<DreamMaker> Read(CancellationToken cancellationToken) => apiClient.Read<DreamMaker>(Routes.DreamMaker, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<DreamMaker> Update(DreamMaker dreamMaker, CancellationToken cancellationToken) => apiClient.Update<DreamMaker, DreamMaker>(Routes.DreamMaker, dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker)), instance.Id, cancellationToken);
	}
}