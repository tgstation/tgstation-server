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
	sealed class DreamDaemonClient : IDreamDaemonClient
	{
		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="DreamDaemonClient"/>.
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// The <see cref="Instance"/> for the <see cref="DreamDaemonClient"/>.
		/// </summary>
		readonly Instance instance;

		/// <summary>
		/// Initializes a new instance of the <see cref="DreamDaemonClient"/> class.
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/>.</param>
		/// <param name="instance">The value of <see cref="instance"/>.</param>
		public DreamDaemonClient(IApiClient apiClient, Instance instance)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.instance = instance ?? throw new ArgumentNullException(nameof(instance));
		}

		/// <inheritdoc />
		public ValueTask Shutdown(CancellationToken cancellationToken) => apiClient.Delete(Routes.DreamDaemon, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<JobResponse> Start(CancellationToken cancellationToken) => apiClient.Create<JobResponse>(Routes.DreamDaemon, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<JobResponse> Restart(CancellationToken cancellationToken) => apiClient.Patch<JobResponse>(Routes.DreamDaemon, instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<DreamDaemonResponse> Read(CancellationToken cancellationToken) => apiClient.Read<DreamDaemonResponse>(
			Routes.DreamDaemon,
			instance.Id!.Value,
			cancellationToken);

		/// <inheritdoc />
		public ValueTask<DreamDaemonResponse> Update(DreamDaemonRequest dreamDaemon, CancellationToken cancellationToken) => apiClient.Update<DreamDaemonRequest, DreamDaemonResponse>(Routes.DreamDaemon, dreamDaemon ?? throw new ArgumentNullException(nameof(dreamDaemon)), instance.Id!.Value, cancellationToken);

		/// <inheritdoc />
		public ValueTask<JobResponse> CreateDump(CancellationToken cancellationToken) => apiClient.Patch<JobResponse>(Routes.Diagnostics, instance.Id!.Value, cancellationToken);
	}
}
