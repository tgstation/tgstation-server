using System;
using System.Net.Http;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client makes a request while the server is starting or stopping.
	/// </summary>
	public sealed class ServiceUnavailableException : ClientException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class.
		/// </summary>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/>.</param>
		public ServiceUnavailableException(HttpResponseMessage responseMessage)
			: base(responseMessage, "The service is unavailable!")
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class.
		/// </summary>
		public ServiceUnavailableException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public ServiceUnavailableException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServiceUnavailableException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public ServiceUnavailableException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
