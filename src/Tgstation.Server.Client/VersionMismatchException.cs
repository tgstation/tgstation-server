using System;
using System.Net.Http;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the API version of the client is not compatible with the server's
	/// </summary>
	public sealed class VersionMismatchException : ApiException
	{
		/// <summary>
		/// Initialize a new instance of the <see cref="VersionMismatchException"/> <see langword="class"/>.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public VersionMismatchException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
		}

		/// <summary>
		/// Construct an <see cref="VersionMismatchException"/>
		/// </summary>
		public VersionMismatchException() { }

		/// <summary>
		/// Construct an <see cref="VersionMismatchException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public VersionMismatchException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="VersionMismatchException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public VersionMismatchException(string message, Exception innerException) : base(message, innerException) { }
	}
}