using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// For logging HTTP requests and responses.
	/// </summary>
	public interface IRequestLogger
	{
		/// <summary>
		/// Log a request.
		/// </summary>
		/// <param name="requestMessage">The <see cref="HttpRequestMessage"/> representing the request.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask LogRequest(HttpRequestMessage requestMessage, CancellationToken cancellationToken);

		/// <summary>
		/// Log a response.
		/// </summary>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> representing the request.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask"/> representing the running operation.</returns>
		ValueTask LogResponse(HttpResponseMessage responseMessage, CancellationToken cancellationToken);
	}
}
