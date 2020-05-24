using System;
using System.Net.Http;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client tries to use a currently unsupported API
	/// </summary>
	public sealed class MethodNotSupportedException : ApiException
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="MethodNotSupportedException"/> <see langword="class"/>.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public MethodNotSupportedException(ErrorMessage? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{ }

		/// <summary>
		/// Construct an <see cref="MethodNotSupportedException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public MethodNotSupportedException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="MethodNotSupportedException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public MethodNotSupportedException(string message, Exception innerException) : base(message, innerException) { }
	}
}