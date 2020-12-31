using System;
using System.Net.Http;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client provides invalid credentials
	/// </summary>
	public sealed class UnauthorizedException : ApiException
	{
		/// <summary>
		/// Construct an <see cref="UnauthorizedException"/>
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> returned by the API.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/>.</param>
		public UnauthorizedException(ErrorMessage? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{ }

		/// <summary>
		/// Intializes a new instance of the <see cref="UnauthorizedException"/> <see langword="class"/>.
		/// </summary>
		public UnauthorizedException() { }

		/// <summary>
		/// Construct an <see cref="UnauthorizedException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public UnauthorizedException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="UnauthorizedException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
	}
}
