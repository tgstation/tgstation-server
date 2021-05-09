using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For sending HTTP requests.
	/// </summary>
	interface IHttpClient : IDisposable
	{
		/// <summary>
		/// The request timeout.
		/// </summary>
		TimeSpan Timeout { get; set; }

		/// <summary>
		/// Send an HTTP request.
		/// </summary>
		/// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="HttpResponseMessage"/> of the request.</returns>
		Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken);
	}
}
