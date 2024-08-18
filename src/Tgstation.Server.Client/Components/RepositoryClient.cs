using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Request;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class RepositoryClient : IRepositoryClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="RepositoryClient"/>.
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="RepositoryClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryClient"/> class.
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		public RepositoryClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient;
			this.instance = instance;
		}

		/// <inheritdoc />
		public ValueTask<RepositoryResponse> Clone(RepositoryCreateRequest repository, CancellationToken cancellationToken) => apiClient.Create<RepositoryCreateRequest, RepositoryResponse>(Routes.Repository, repository, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<RepositoryResponse> Delete(CancellationToken cancellationToken) => apiClient.Delete<RepositoryResponse>(Routes.Repository, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<RepositoryResponse> Read(CancellationToken cancellationToken) => apiClient.Read<RepositoryResponse>(Routes.Repository, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<RepositoryResponse> Update(RepositoryUpdateRequest repository, CancellationToken cancellationToken) => apiClient.Update<RepositoryUpdateRequest, RepositoryResponse>(Routes.Repository, repository ?? throw new ArgumentNullException(nameof(repository)), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<RepositoryResponse> Reclone(CancellationToken cancellationToken) => apiClient.Patch<RepositoryResponse>(Routes.Repository, instance.Id!.Value, cancellationToken);
	}
}
