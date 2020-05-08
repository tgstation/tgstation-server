using System;
using System.Net.Http;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when a response is received that did not deserialize to one of the expected <see cref="Api.Models"/>
	/// </summary>
	sealed class UnrecognizedResponseException : ClientException
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="UnrecognizedResponseException"/> <see langword="class"/>.
		/// </summary>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/>.</param>
		public UnrecognizedResponseException(HttpResponseMessage responseMessage) : base(responseMessage, "Unrecognized response body!")
		{ }

		/// <summary>
		/// Construct a <see cref="UnrecognizedResponseException"/>
		/// </summary>
		public UnrecognizedResponseException() { }

		/// <summary>
		/// Construct an <see cref="UnrecognizedResponseException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public UnrecognizedResponseException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="UnrecognizedResponseException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public UnrecognizedResponseException(string message, Exception innerException) : base(message, innerException) { }
	}
}
