using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

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
		public Task<Job> Compile(CancellationToken cancellationToken) => ApiClient.Create<Job>(Routes.DreamMaker, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<CompileJob> GetCompileJob(EntityId compileJob, CancellationToken cancellationToken) => ApiClient.Read<CompileJob>(Routes.SetID(Routes.DreamMaker, compileJob?.Id ?? throw new ArgumentNullException(nameof(compileJob))), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<IReadOnlyList<EntityId>> ListCompileJobs(PaginationSettings? paginationSettings, CancellationToken cancellationToken)
			=> ReadPaged<EntityId>(paginationSettings, Routes.ListRoute(Routes.DreamMaker), instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<DreamMaker> Read(CancellationToken cancellationToken) => ApiClient.Read<DreamMaker>(Routes.DreamMaker, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<DreamMaker> Update(DreamMaker dreamMaker, CancellationToken cancellationToken) => ApiClient.Update<DreamMaker, DreamMaker>(Routes.DreamMaker, dreamMaker ?? throw new ArgumentNullException(nameof(dreamMaker)), instance.Id, cancellationToken);
	}
}
