using System;
using System.Net.Http;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client provides invalid credentials.
	/// </summary>
	public sealed class UnauthorizedException : ApiException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> returned by the API.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/>.</param>
		public UnauthorizedException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
		/// </summary>
		public UnauthorizedException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public UnauthorizedException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UnauthorizedException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public UnauthorizedException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
