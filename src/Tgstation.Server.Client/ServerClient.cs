using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class ServerClient : IServerClient
	{
		/// <inheritdoc />
		public Uri Url => apiClient.Url;

		/// <inheritdoc />
		public TokenResponse Token
		{
			get => token;
			set
			{
				token = value ?? throw new InvalidOperationException("Cannot set a null Token!");
				apiClient.Headers = new ApiHeaders(apiClient.Headers.UserAgent!, token.Bearer!);
			}
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

		/// <summary>
		/// The <see cref="IApiClient"/> for the <see cref="ServerClient"/>.
		/// </summary>
		readonly IApiClient apiClient;

		/// <summary>
		/// Backing field for <see cref="Token"/>.
		/// </summary>
		TokenResponse token;

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerClient"/> class.
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/>.</param>
		/// <param name="token">The value of <see cref="Token"/>.</param>
		public ServerClient(IApiClient apiClient, TokenResponse token)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.token = token ?? throw new ArgumentNullException(nameof(token));

			if (Token.Bearer != apiClient.Headers.Token)
				throw new ArgumentOutOfRangeException(nameof(token), token, "Provided token does not match apiClient headers!");

			Instances = new InstanceManagerClient(apiClient);
			Users = new UsersClient(apiClient);
			Administration = new AdministrationClient(apiClient);
			Groups = new UserGroupsClient(apiClient);
		}

		/// <inheritdoc />
		public void Dispose() => apiClient.Dispose();

		/// <inheritdoc />
		public ValueTask<ServerInformationResponse> ServerInformation(CancellationToken cancellationToken) => apiClient.Read<ServerInformationResponse>(Routes.Root, cancellationToken);

		/// <inheritdoc />
		public void AddRequestLogger(IRequestLogger requestLogger) => apiClient.AddRequestLogger(requestLogger);
	}
}
