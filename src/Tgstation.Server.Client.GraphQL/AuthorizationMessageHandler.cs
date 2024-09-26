using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Client.GraphQL
{
	/// <summary>
	/// <see cref="DelegatingHandler"/> that applies the <see cref="AuthenticationHeaderValue"/>.
	/// </summary>
	sealed class AuthorizationMessageHandler : DelegatingHandler
	{
		/// <summary>
		/// The <see cref="AsyncLocal{T}"/> <see cref="AuthenticationHeaderValue"/> to be applied.
		/// </summary>
		public static AsyncLocal<AuthenticationHeaderValue?> Header { get; } = new AsyncLocal<AuthenticationHeaderValue?>();

		/// <summary>
		/// <see langword="class"/> override for <see cref="Header"/>.
		/// </summary>
		readonly AuthenticationHeaderValue? headerOverride;

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorizationMessageHandler"/> class.
		/// </summary>
		/// <param name="headerOverride">The value of <see cref="headerOverride"/>.</param>
		public AuthorizationMessageHandler(AuthenticationHeaderValue? headerOverride)
		{
			this.headerOverride = headerOverride;
		}

		/// <inheritdoc />
		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			var currentAuthHeader = headerOverride ?? Header.Value;
			if (currentAuthHeader != null)
				request.Headers.Authorization = currentAuthHeader;

			return base.SendAsync(request, cancellationToken);
		}
	}
}
