using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Main client for communicating with a server.
	/// </summary>
	public interface IRestServerClient : IAsyncDisposable
	{
		/// <summary>
		/// The connected server's root <see cref="Uri"/>.
		/// </summary>
		Uri Url { get; }

		/// <summary>
		/// The <see cref="Token"/> used to access the server.
		/// </summary>
		TokenResponse Token { get; set; }

		/// <summary>
		/// The connection timeout.
		/// </summary>
		TimeSpan Timeout { get; set; }

		/// <summary>
		/// Access the <see cref="IInstanceManagerClient"/>.
		/// </summary>
		IInstanceManagerClient Instances { get; }

		/// <summary>
		/// Access the <see cref="IAdministrationClient"/>.
		/// </summary>
		IAdministrationClient Administration { get; }

		/// <summary>
		/// Access the <see cref="IUsersClient"/>.
		/// </summary>
		IUsersClient Users { get; }

		/// <summary>
		/// Access the <see cref="IUserGroupsClient"/>.
		/// </summary>
		IUserGroupsClient Groups { get; }

		/// <summary>
		/// Access the <see cref="ITransferClient"/>.
		/// </summary>
		/// <remarks>Most client methods handle transfers in their invocations. There is rarely any reason to use the <see cref="ITransferClient"/> directly.</remarks>
		ITransferClient Transfer { get; }

		/// <summary>
		/// The <see cref="ServerInformationResponse"/> of the <see cref="IRestServerClient"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="ServerInformationResponse"/> of the target server.</returns>
		ValueTask<ServerInformationResponse> ServerInformation(CancellationToken cancellationToken);

		/// <summary>
		/// Subscribe to all job updates available to the <see cref="IRestServerClient"/>.
		/// </summary>
		/// <param name="jobsReceiver">The <see cref="IJobsHub"/> to use to subscribe to updates.</param>
		/// <param name="retryPolicy">The optional <see cref="IRetryPolicy"/> to use for the backing connection. The default retry policy waits for 1, 2, 4, 8, and 16 seconds, then 30s repeatedly.</param>
		/// <param name="loggingConfigureAction">The optional <see cref="Action{T1}"/> used to configure a <see cref="ILoggingBuilder"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>An <see cref="IAsyncDisposable"/> representing the lifetime of the subscription.</returns>
		ValueTask<IAsyncDisposable> SubscribeToJobUpdates(
			IJobsHub jobsReceiver,
			IRetryPolicy? retryPolicy = null,
			Action<ILoggingBuilder>? loggingConfigureAction = null,
			CancellationToken cancellationToken = default);

		/// <summary>
		/// Adds a <paramref name="requestLogger"/> to the request pipeline.
		/// </summary>
		/// <param name="requestLogger">The <see cref="IRequestLogger"/> to add.</param>
		void AddRequestLogger(IRequestLogger requestLogger);
	}
}
