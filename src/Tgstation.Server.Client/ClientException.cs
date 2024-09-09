using System;
using System.Net;
using System.Net.Http;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Exceptions thrown by <see cref="IRestServerClient"/>s.
	/// </summary>
	public abstract class ClientException : Exception
	{
		/// <summary>
		/// The <see cref="HttpStatusCode"/> of the <see cref="ClientException"/>.
		/// </summary>
		public HttpResponseMessage? ResponseMessage { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ClientException"/> class.
		/// </summary>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> that generated the <see cref="ClientException"/>.</param>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		protected ClientException(HttpResponseMessage responseMessage, string message)
			: base(message)
		{
			ResponseMessage = responseMessage ?? throw new ArgumentNullException(nameof(responseMessage));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ClientException"/> class.
		/// </summary>
		protected ClientException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ClientException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		protected ClientException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ClientException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		protected ClientException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
