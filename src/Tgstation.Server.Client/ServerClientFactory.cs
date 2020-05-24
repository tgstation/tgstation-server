using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	public sealed class ServerClientFactory : IServerClientFactory
	{
		/// <summary>
		/// The <see cref="IApiClientFactory"/> for the <see cref="ServerClientFactory"/>
		/// </summary>
		static readonly IApiClientFactory ApiClientFactory = new ApiClientFactory();

		/// <summary>
		/// The <see cref="ProductHeaderValue"/> for the <see cref="ServerClientFactory"/>
		/// </summary>
		readonly ProductHeaderValue productHeaderValue;

		/// <summary>
		/// Construct a <see cref="ServerClientFactory"/>
		/// </summary>
		/// <param name="productHeaderValue">The value of <see cref="productHeaderValue"/></param>
		public ServerClientFactory(ProductHeaderValue productHeaderValue)
		{
			this.productHeaderValue = productHeaderValue ?? throw new ArgumentNullException(nameof(productHeaderValue));
		}

		/// <inheritdoc />
		public async Task<IServerClient> CreateFromLogin(
			Uri host,
			string username,
			string password,
			IEnumerable<IRequestLogger>? requestLoggers = null,
			TimeSpan? timeout = null,
			CancellationToken cancellationToken = default)
		{
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			if (username == null)
				throw new ArgumentNullException(nameof(username));
			if (password == null)
				throw new ArgumentNullException(nameof(password));

			requestLoggers ??= Enumerable.Empty<IRequestLogger>();

			Token token;
			using (var api = ApiClientFactory.CreateApiClient(host, new ApiHeaders(productHeaderValue, username, password)))
			{
				foreach (var requestLogger in requestLoggers)
					api.AddRequestLogger(requestLogger);

				if (timeout.HasValue)
					api.Timeout = timeout.Value;
				token = await api.Update<Token>(Routes.Root, cancellationToken).ConfigureAwait(false);
			}

			var client = CreateFromToken(host, token);
			if (timeout.HasValue)
				client.Timeout = timeout.Value;

			return client;
		}

		/// <inheritdoc />
		public IServerClient CreateFromToken(Uri host, Token token)
		{
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			if (token == null)
				throw new ArgumentNullException(nameof(token));
			if (token.Bearer == null)
				throw new ArgumentException("token.Bearer should not be null!", nameof(token));

			return new ServerClient(ApiClientFactory.CreateApiClient(host, new ApiHeaders(productHeaderValue, token.Bearer)), token);
		}
	}
}
