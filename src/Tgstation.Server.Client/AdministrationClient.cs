using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class AdministrationClient : IAdministrationClient
	{
		/// <summary>
		/// The <see cref="apiClient"/> for the <see cref="AdministrationClient"/>
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// Construct an <see cref="AdministrationClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		public AdministrationClient(IApiClient apiClient)
		{
			this.apiClient = apiClient;
		}

		/// <inheritdoc />
		public Task<Administration> Read(CancellationToken cancellationToken) => apiClient.Read<Administration>(Routes.Administration, cancellationToken);

		/// <inheritdoc />
		public Task Update(Administration administration, CancellationToken cancellationToken) => apiClient.Update(Routes.Administration, administration, cancellationToken);
	}
}