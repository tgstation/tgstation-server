using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class RepositoryClient : IRepositoryClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="RepositoryClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="RepositoryClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="RepositoryClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="instance"/></param>
		public RepositoryClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient;
			this.instance = instance;
		}

		/// <inheritdoc />
		public Task<RepositoryResponse> Clone(RepositoryCreateRequest repository, CancellationToken cancellationToken) => apiClient.Create<RepositoryCreateRequest, RepositoryResponse>(Routes.Repository, repository, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<RepositoryResponse> Delete(CancellationToken cancellationToken) => apiClient.Delete<RepositoryResponse>(Routes.Repository, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<RepositoryResponse> Read(CancellationToken cancellationToken) => apiClient.Read<RepositoryResponse>(Routes.Repository, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public Task<RepositoryResponse> Update(RepositoryUpdateRequest repository, CancellationToken cancellationToken) => apiClient.Update<RepositoryUpdateRequest, RepositoryResponse>(Routes.Repository, repository ?? throw new ArgumentNullException(nameof(repository)), instance.Id!.Value, cancellationToken);
	}
}
