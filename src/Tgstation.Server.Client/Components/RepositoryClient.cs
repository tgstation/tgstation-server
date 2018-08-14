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
		/// <param name="instance"></param>
		public RepositoryClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient;
			this.instance = instance;
		}

		/// <inheritdoc />
		public Task Delete(CancellationToken cancellationToken) => apiClient.Delete(Routes.Repository, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<Repository> Read(CancellationToken cancellationToken) => apiClient.Read<Repository>(Routes.Repository, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<Repository> Update(Repository repository, CancellationToken cancellationToken) => apiClient.Update<Repository, Repository>(Routes.Repository, repository, instance.Id, cancellationToken);
	}
}