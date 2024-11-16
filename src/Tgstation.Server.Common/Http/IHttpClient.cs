#if NETSTANDARD2_0_OR_GREATER
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Common.Http
{
	/// <summary>
	/// For sending HTTP requests.
	/// </summary>
	public interface IHttpClient : IDisposable
	{
		/// <summary>
		/// The request timeout.
		/// </summary>
		TimeSpan Timeout { get; set; }

		/// <summary>
		/// The <see cref="HttpRequestHeaders"/> used on every request.
		/// </summary>
		HttpRequestHeaders DefaultRequestHeaders { get; }

		/// <summary>
		/// Send an HTTP request.
		/// </summary>
		/// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
		/// <param name="completionOption">The <see cref="HttpCompletionOption"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="HttpResponseMessage"/> of the request.</returns>
		Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken);
	}
}
#endif
