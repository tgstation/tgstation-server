using System;
using System.Net.Http;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client provides invalid credentials.
	/// </summary>
	public sealed class RequestTimeoutException : ClientException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RequestTimeoutException"/> class.
		/// </summary>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public RequestTimeoutException(HttpResponseMessage responseMessage)
			: base(responseMessage, "The request timed out!")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestTimeoutException"/> class.
		/// </summary>
		public RequestTimeoutException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestTimeoutException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public RequestTimeoutException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RequestTimeoutException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public RequestTimeoutException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
