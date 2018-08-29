using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client.Components
{
	/// <inheritdoc />
	sealed class ByondClient : IByondClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="ByondClient"/>
		/// </summary>
		readonly IApiClient apiClient;
		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="ByondClient"/>
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Construct a <see cref="ByondClient"/>
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/></param>
		/// <param name="instance">The value of <see cref="Instance"/></param>
		public ByondClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public Task<Byond> Read(CancellationToken cancellationToken) => apiClient.Read<Byond>(Routes.Byond, instance.Id, cancellationToken);

		/// <inheritdoc />
		public Task<Byond> Update(Byond byond, CancellationToken cancellationToken) => apiClient.Update<Byond, Byond>(Routes.Byond, byond ?? throw new ArgumentNullException(nameof(byond)), instance.Id, cancellationToken);
	}
}