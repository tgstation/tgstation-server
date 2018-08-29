using System;
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
		static readonly IApiClientFactory apiClientFactory = new ApiClientFactory();

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
		public async Task<IServerClient> CreateServerClient(Uri host, string username, string password, TimeSpan timeout, CancellationToken cancellationToken)
		{
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			if (username == null)
				throw new ArgumentNullException(nameof(username));
			if (password == null)
				throw new ArgumentNullException(nameof(password));

			Token token;
			using (var api = apiClientFactory.CreateApiClient(host, new ApiHeaders(productHeaderValue, username, password)))
			{
				if (timeout != default)
					api.Timeout = timeout;
				token = await api.Update<Token>(Routes.Root, cancellationToken).ConfigureAwait(false);
			}
			return CreateServerClient(host, token, timeout);
		}

		/// <inheritdoc />
		public IServerClient CreateServerClient(Uri host, Token token, TimeSpan timeout)
		{
			if (host == null)
				throw new ArgumentNullException(nameof(host));
			if (token == null)
				throw new ArgumentNullException(nameof(token));
			var result = new ServerClient(apiClientFactory.CreateApiClient(host, new ApiHeaders(productHeaderValue, token.Bearer)), token);
			if (timeout != default)
				result.Timeout = timeout;
			return result;
		}
	}
}
