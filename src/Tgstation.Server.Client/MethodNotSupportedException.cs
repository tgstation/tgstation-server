using System;
using System.Net.Http;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client tries to use a currently unsupported API.
	/// </summary>
	public sealed class MethodNotSupportedException : ApiException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MethodNotSupportedException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public MethodNotSupportedException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodNotSupportedException"/> class.
		/// </summary>
		public MethodNotSupportedException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodNotSupportedException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public MethodNotSupportedException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MethodNotSupportedException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public MethodNotSupportedException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
