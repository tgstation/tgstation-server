using System;
using System.Net.Http;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the server returns a bad request response if the <see cref="ApiException.ErrorCode"/> is present. The server returned an unknown reponse otherwise.
	/// </summary>
	public sealed class ApiConflictException : ApiException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ApiConflictException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public ApiConflictException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiConflictException"/> class.
		/// </summary>
		public ApiConflictException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiConflictException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public ApiConflictException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiConflictException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public ApiConflictException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
