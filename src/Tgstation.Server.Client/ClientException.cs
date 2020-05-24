using System;
using System.Net;
using System.Net.Http;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Exceptions thrown by <see cref="IServerClient"/>s
	/// </summary>
	public abstract class ClientException : Exception
	{
		/// <summary>
		/// The <see cref="HttpStatusCode"/> of the <see cref="ClientException"/>
		/// </summary>
		public HttpResponseMessage? ResponseMessage { get; }

		/// <summary>
		/// Initialize a new instance of the <see cref="ClientException"/> <see langword="class"/>.
		/// </summary>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> that generated the <see cref="ClientException"/>.</param>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		protected ClientException(HttpResponseMessage responseMessage, string message) : base(message)
		{
			ResponseMessage = responseMessage ?? throw new ArgumentNullException(nameof(responseMessage));
		}

		/// <summary>
		/// Construct a <see cref="ClientException"/>
		/// </summary>
		protected ClientException() { }

		/// <summary>
		/// Construct a <see cref="ClientException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		protected ClientException(string message) : base(message) { }

		/// <summary>
		/// Construct a <see cref="ClientException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		protected ClientException(string message, Exception innerException) : base(message, innerException) { }
	}
}
