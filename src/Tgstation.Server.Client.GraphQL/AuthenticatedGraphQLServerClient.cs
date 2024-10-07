using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StrawberryShake;

using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Client.GraphQL
{
	/// <inheritdoc cref="IAuthenticatedGraphQLServerClient" />
	sealed class AuthenticatedGraphQLServerClient : GraphQLServerClient, IAuthenticatedGraphQLServerClient
	{
		/// <inheritdoc />
		public ITransferClient TransferClient => restClient!.Transfer;

		/// <summary>
		/// A <see cref="Func{T, TResult}"/> that takes a bearer token as input and outputs a <see cref="ITransferClient"/> that uses it.
		/// </summary>
		readonly Func<string, IRestServerClient>? getRestClientForToken;

		/// <summary>
		/// The current <see cref="IRestServerClient"/>.
		/// </summary>
		IRestServerClient? restClient;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticatedGraphQLServerClient"/> class.
		/// </summary>
		/// <param name="graphQLClient">The <see cref="IGraphQLClient"/> to use.</param>
		/// <param name="serviceProvider">The <see cref="IAsyncDisposable"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="restClient">The value of <see cref="restClient"/>.</param>
		public AuthenticatedGraphQLServerClient(
			IGraphQLClient graphQLClient,
			IAsyncDisposable serviceProvider,
			ILogger<GraphQLServerClient> logger,
			IRestServerClient restClient)
			: base(graphQLClient, serviceProvider, logger)
		{
			this.restClient = restClient ?? throw new ArgumentNullException(nameof(restClient));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthenticatedGraphQLServerClient"/> class.
		/// </summary>
		/// <param name="graphQLClient">The <see cref="IGraphQLClient"/> to use.</param>
		/// <param name="serviceProvider">The <see cref="IAsyncDisposable"/> to use.</param>
		/// <param name="logger">The <see cref="ILogger"/> to use.</param>
		/// <param name="setAuthenticationHeader">The <see cref="Action{T}"/> to call to set the async local <see cref="AuthenticationHeaderValue"/> for requests.</param>
		/// <param name="basicCredentialsHeader">The basic <see cref="AuthenticationHeaderValue"/> to use for reauthentication.</param>
		/// <param name="loginResult">The <see cref="ILoginResult"/> <see cref="IOperationResult{TResultData}"/> containing the initial JWT to use.</param>
		/// <param name="getRestClientForToken">The value of <see cref="getRestClientForToken"/>.</param>
		public AuthenticatedGraphQLServerClient(
			IGraphQLClient graphQLClient,
			IAsyncDisposable serviceProvider,
			ILogger<GraphQLServerClient> logger,
			Action<AuthenticationHeaderValue> setAuthenticationHeader,
			AuthenticationHeaderValue? basicCredentialsHeader,
			IOperationResult<ILoginResult> loginResult,
			Func<string, IRestServerClient> getRestClientForToken)
			: base(
				  graphQLClient,
				  serviceProvider,
				  logger,
				  setAuthenticationHeader,
				  basicCredentialsHeader,
				  loginResult)
		{
			this.getRestClientForToken = getRestClientForToken ?? throw new ArgumentNullException(nameof(getRestClientForToken));
			restClient = getRestClientForToken(loginResult.Data!.Login.LoginResult!.Bearer.EncodedToken);
		}

		/// <inheritdoc />
		public sealed override ValueTask DisposeAsync()
#pragma warning disable CA2012 // Use ValueTasks correctly
			=> ValueTaskExtensions.WhenAll(
				base.DisposeAsync(),
				restClient!.DisposeAsync());
#pragma warning restore CA2012 // Use ValueTasks correctly

		/// <inheritdoc />
		protected sealed override async ValueTask<AuthenticationHeaderValue> CreateUpdatedAuthenticationHeader(string bearer)
		{
			var baseTask = base.CreateUpdatedAuthenticationHeader(bearer);
			if (restClient != null)
				await restClient.DisposeAsync().ConfigureAwait(false);

			if (getRestClientForToken != null)
				restClient = getRestClientForToken(bearer);

			return await baseTask.ConfigureAwait(false);
		}
	}
}
