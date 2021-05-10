using System;
using System.Net.Http;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client performs an action that would result in data conflict.
	/// </summary>
	public sealed class ConflictException : ApiException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public ConflictException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictException"/> class.
		/// </summary>
		public ConflictException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public ConflictException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConflictException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public ConflictException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
