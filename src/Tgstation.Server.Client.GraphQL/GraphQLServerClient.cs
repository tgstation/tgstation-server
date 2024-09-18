using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using StrawberryShake;

using Tgstation.Server.Api;

namespace Tgstation.Server.Client.GraphQL
{
	/// <inheritdoc />
	class GraphQLServerClient : IGraphQLServerClient
	{
		/// <summary>
		/// If the <see cref="GraphQLServerClient"/> was initially authenticated.
		/// </summary>
		[MemberNotNullWhen(true, nameof(setAuthenticationHeader))]
		[MemberNotNullWhen(true, nameof(bearerCredentialsTask))]
		bool Authenticated => basicCredentialsHeader != null;

		/// <summary>
		/// If the <see cref="GraphQLServerClient"/> supports reauthentication.
		/// </summary>
		[MemberNotNullWhen(true, nameof(bearerCredentialsHeaderTaskLock))]
		[MemberNotNullWhen(true, nameof(basicCredentialsHeader))]
		bool CanReauthenticate => basicCredentialsHeader != null;

		/// <summary>
		/// The <see cref="IGraphQLClient"/> for the <see cref="GraphQLServerClient"/>.
		/// </summary>
		readonly IGraphQLClient graphQLClient;

		/// <summary>
		/// The <see cref="IAsyncDisposable"/> to be <see cref="DisposeAsync"/>'d with the <see cref="GraphQLServerClient"/>.
		/// </summary>
		readonly IAsyncDisposable serviceProvider;

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GraphQLServerClient"/>.
		/// </summary>
		readonly ILogger<GraphQLServerClient> logger;

		/// <summary>
		/// The <see cref="Action{T}"/> which sets the <see cref="AuthenticationHeaderValue"/> for HTTP request in the current async context.
		/// </summary>
		readonly Action<AuthenticationHeaderValue>? setAuthenticationHeader;

		/// <summary>
		/// The <see cref="AuthenticationHeaderValue"/> containing the authenticated user's password credentials.
		/// </summary>
		readonly AuthenticationHeaderValue? basicCredentialsHeader;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> used to synchronize access to <see cref="bearerCredentialsTask"/>.
		/// </summary>
		readonly object? bearerCredentialsHeaderTaskLock;

		/// <summary>
		/// A <see cref="Task{TResult}"/> resulting in a <see cref="ValueTuple{T1, T2}"/> containing the current <see cref="AuthenticationHeaderValue"/> for the <see cref="ApiHeaders.BearerAuthenticationScheme"/> and the <see cref="DateTime"/> it expires.
		/// </summary>
		Task<(AuthenticationHeaderValue Header, DateTime Exp)?>? bearerCredentialsTask;

		/// <summary>
		/// Throws an <see cref="AuthenticationException"/> for a login error that previously occured outside of the current call context.
		/// </summary>
		/// <exception cref="AuthenticationException">Always thrown.</exception>
		[DoesNotReturn]
		static void ThrowOtherCallerFailedAuthException()
			=> throw new AuthenticationException("Another caller failed to authenticate!");

		/// <summary>
		/// Checks if a given <paramref name="operationResult"/> errored out with authentication errors.
		/// </summary>
		/// <param name="operationResult">The <see cref="IOperationResult"/>.</param>
		/// <returns><see langword="true"/> if <paramref name="operationResult"/> errored due to authentication issues, <see langword="false"/> otherwise.</returns>
		static bool IsAuthenticationError(IOperationResult operationResult)
			=> operationResult.Data == null
				&& operationResult.Errors.Any(
					error => error.Extensions?.TryGetValue(
						   "code",
						   out object? codeExtension) == true
					&& codeExtension is string codeExtensionString
					&& codeExtensionString == "AUTH_NOT_AUTHENTICATED");

		/// <summary>
		/// Initializes a new instance of the <see cref="GraphQLServerClient"/> class.
		/// </summary>
		/// <param name="graphQLClient">The value of <see cref="graphQLClient"/>.</param>
		/// <param name="serviceProvider">The value of <see cref="serviceProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		public GraphQLServerClient(
			IGraphQLClient graphQLClient,
			IAsyncDisposable serviceProvider,
			ILogger<GraphQLServerClient> logger)
		{
			this.graphQLClient = graphQLClient ?? throw new ArgumentNullException(nameof(graphQLClient));
			this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="GraphQLServerClient"/> class.
		/// </summary>
		/// <param name="graphQLClient">The value of <see cref="graphQLClient"/>.</param>
		/// <param name="serviceProvider">The value of <see cref="serviceProvider"/>.</param>
		/// <param name="logger">The value of <see cref="logger"/>.</param>
		/// <param name="setAuthenticationHeader">The value of <see cref="setAuthenticationHeader"/>.</param>
		/// <param name="basicCredentialsHeader">The value of <see cref="basicCredentialsHeader"/>.</param>
		/// <param name="loginResult">The <see cref="ILoginResult"/> <see cref="IOperationResult{TResultData}"/> containing the initial JWT to use.</param>
		protected GraphQLServerClient(
			IGraphQLClient graphQLClient,
			IAsyncDisposable serviceProvider,
			ILogger<GraphQLServerClient> logger,
			Action<AuthenticationHeaderValue> setAuthenticationHeader,
			AuthenticationHeaderValue? basicCredentialsHeader,
			IOperationResult<ILoginResult> loginResult)
			: this(graphQLClient, serviceProvider, logger)
		{
			this.setAuthenticationHeader = setAuthenticationHeader ?? throw new ArgumentNullException(nameof(setAuthenticationHeader));
			ArgumentNullException.ThrowIfNull(loginResult);
			this.basicCredentialsHeader = basicCredentialsHeader;

			var task = CreateCredentialsTuple(loginResult);
			if (!task.IsCompleted)
				throw new InvalidOperationException($"Expected {nameof(CreateCredentialsTuple)} to not await in constructor!");

			bearerCredentialsTask = Task.FromResult<(AuthenticationHeaderValue Header, DateTime Exp)?>(task.Result);

			if (Authenticated)
				bearerCredentialsHeaderTaskLock = new object();
		}

		/// <inheritdoc />
		public virtual ValueTask DisposeAsync() => serviceProvider.DisposeAsync();

		/// <inheritdoc />
		public ValueTask<IOperationResult<TResultData>> RunOperationAsync<TResultData>(Func<IGraphQLClient, ValueTask<IOperationResult<TResultData>>> queryExector, CancellationToken cancellationToken)
			where TResultData : class
		{
			ArgumentNullException.ThrowIfNull(queryExector);
			return WrapAuthentication(queryExector, cancellationToken);
		}

		/// <inheritdoc />
		public ValueTask<IOperationResult<TResultData>> RunOperation<TResultData>(Func<IGraphQLClient, Task<IOperationResult<TResultData>>> queryExector, CancellationToken cancellationToken)
			where TResultData : class
		{
			ArgumentNullException.ThrowIfNull(queryExector);
			return WrapAuthentication(async localClient => await queryExector(localClient), cancellationToken);
		}

		/// <summary>
		/// Create a <see cref="AuthenticationHeaderValue"/> from a given <paramref name="bearer"/> token.
		/// </summary>
		/// <param name="bearer">The <see cref="ApiHeaders.BearerAuthenticationScheme"/> <see cref="string"/>.</param>
		/// <returns>A new <see cref="AuthenticationHeaderValue"/>.</returns>
		protected virtual ValueTask<AuthenticationHeaderValue> CreateUpdatedAuthenticationHeader(string bearer)
			=> ValueTask.FromResult(
				new AuthenticationHeaderValue(
					ApiHeaders.BearerAuthenticationScheme,
					bearer));

		/// <summary>
		/// Executes a given <paramref name="operationExecutor"/>, potentially accounting for authentication issues.
		/// </summary>
		/// <typeparam name="TResultData">The <see cref="Type"/> of the <see cref="IOperationResult{TResultData}"/>'s <see cref="IOperationResult{TResultData}.Data"/>.</typeparam>
		/// <param name="operationExecutor">A <see cref="Func{T, TResult}"/> which executes a single query on a given <see cref="IGraphQLClient"/> and returns a <see cref="ValueTask{TResult}"/> resulting in the <typeparamref name="TResultData"/> <see cref="IOperationResult{TResultData}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IOperationResult{TResultData}"/>.</returns>
		async ValueTask<IOperationResult<TResultData>> WrapAuthentication<TResultData>(Func<IGraphQLClient, ValueTask<IOperationResult<TResultData>>> operationExecutor, CancellationToken cancellationToken)
			where TResultData : class
		{
			if (!Authenticated)
				return await operationExecutor(graphQLClient).ConfigureAwait(false);

			var tuple = await bearerCredentialsTask.ConfigureAwait(false);
			if (!tuple.HasValue)
				ThrowOtherCallerFailedAuthException();

			async ValueTask<AuthenticationHeaderValue> Reauthenticate(AuthenticationHeaderValue currentToken, CancellationToken cancellationToken)
			{
				if (!CanReauthenticate)
					throw new AuthenticationException("Authentication expired or invalid and cannot re-authenticate.");

				TaskCompletionSource<(AuthenticationHeaderValue Header, DateTime Exp)?>? tcs = null;
				do
				{
					var bearerCredentialsTaskLocal = bearerCredentialsTask;
					if (!bearerCredentialsTaskLocal!.IsCompleted)
					{
						var currentTuple = await bearerCredentialsTaskLocal.ConfigureAwait(false);
						if (!currentTuple.HasValue)
							ThrowOtherCallerFailedAuthException();

						return currentTuple.Value.Header;
					}

					lock (bearerCredentialsHeaderTaskLock!)
					{
						if (bearerCredentialsTask == bearerCredentialsTaskLocal)
						{
							var result = bearerCredentialsTaskLocal.Result;
							if (result?.Header != currentToken)
							{
								if (!result.HasValue)
									ThrowOtherCallerFailedAuthException();

								return result.Value.Header;
							}

							tcs = new TaskCompletionSource<(AuthenticationHeaderValue, DateTime)?>();
							bearerCredentialsTask = tcs.Task;
						}
					}
				}
				while (tcs == null);

				setAuthenticationHeader!(basicCredentialsHeader!);
				var loginResult = await graphQLClient.Login.ExecuteAsync(cancellationToken).ConfigureAwait(false);
				try
				{
					var tuple = await CreateCredentialsTuple(loginResult).ConfigureAwait(false);
					tcs.SetResult(tuple);
					return tuple.Header;
				}
				catch (AuthenticationException)
				{
					tcs.SetResult(null);
					throw;
				}
			}

			var (currentAuthHeader, expires) = tuple.Value;
			if (expires <= DateTimeOffset.UtcNow)
				currentAuthHeader = await Reauthenticate(currentAuthHeader, cancellationToken).ConfigureAwait(false);

			setAuthenticationHeader(currentAuthHeader);

			var operationResult = await operationExecutor(graphQLClient);

			if (IsAuthenticationError(operationResult))
			{
				currentAuthHeader = await Reauthenticate(currentAuthHeader, cancellationToken).ConfigureAwait(false);
				setAuthenticationHeader(currentAuthHeader);
				return await operationExecutor(graphQLClient);
			}

			return operationResult;
		}

		/// <summary>
		/// Attempt to create the <see cref="ValueTuple{T1, T2}"/> for <see cref="bearerCredentialsTask"/>.
		/// </summary>
		/// <param name="loginResult">The <see cref="ILoginResult"/> <see cref="IOperationResult{TResultData}"/> to process.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new credentials <see cref="ValueTuple{T1, T2}"/>.</returns>
		/// <exception cref="AuthenticationException">Thrown if the <paramref name="loginResult"/> errored.</exception>
		async ValueTask<(AuthenticationHeaderValue Header, DateTime Exp)> CreateCredentialsTuple(IOperationResult<ILoginResult> loginResult)
		{
			var bearer = loginResult.EnsureSuccess(logger);

			var header = await CreateUpdatedAuthenticationHeader(bearer.EncodedToken);

			return (Header: header, Exp: bearer.ValidTo);
		}
	}
}
