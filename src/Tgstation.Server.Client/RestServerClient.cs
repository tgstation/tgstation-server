using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class RestServerClient : IRestServerClient
	{
		/// <inheritdoc />
		public Uri Url => apiClient.Url;

		/// <inheritdoc />
		public TokenResponse Token
		{
			get => apiClient.Headers.Token ?? throw new InvalidOperationException("apiClient.Headers.Token was null!");
			set => apiClient.Headers = new ApiHeaders(apiClient.Headers.UserAgent!, value);
		}

		/// <inheritdoc />
		public TimeSpan Timeout
		{
			get => apiClient.Timeout;
			set => apiClient.Timeout = value;
		}

		/// <inheritdoc />
		public IInstanceManagerClient Instances { get; }

		/// <inheritdoc />
		public IAdministrationClient Administration { get; }

		/// <inheritdoc />
		public IUsersClient Users { get; }

		/// <inheritdoc />
		public IUserGroupsClient Groups { get; }

		/// <inheritdoc />
		public ITransferClient Transfer => apiClient;

		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="RestServerClient"/>.
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="RestServerClient"/> class.
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/>.</param>
		public RestServerClient(IApiClient apiClient)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

			Instances = new InstanceManagerClient(apiClient);
			Users = new UsersClient(apiClient);
			Administration = new AdministrationClient(apiClient);
			Groups = new UserGroupsClient(apiClient);
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync() => apiClient.DisposeAsync();

		/// <inheritdoc />
		public ValueTask<ServerInformationResponse> ServerInformation(CancellationToken cancellationToken) => apiClient.Read<ServerInformationResponse>(Routes.ApiRoot, cancellationToken);

		/// <inheritdoc />
		public void AddRequestLogger(IRequestLogger requestLogger) => apiClient.AddRequestLogger(requestLogger);

		/// <inheritdoc />
		public ValueTask<IAsyncDisposable> SubscribeToJobUpdates(
			IJobsHub jobsReceiver,
			IRetryPolicy? retryPolicy,
			Action<ILoggingBuilder>? loggingConfigureAction,
			CancellationToken cancellationToken)
			=> apiClient.CreateHubConnection(jobsReceiver, retryPolicy, loggingConfigureAction, cancellationToken);
	}
}
