using System;
using System.Net.Http;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when an error occurs in the server.
	/// </summary>
	public sealed class ServerErrorException : ApiException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ServerErrorException"/> class.
		/// </summary>
		public ServerErrorException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerErrorException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public ServerErrorException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerErrorException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public ServerErrorException(string message) : base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerErrorException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public ServerErrorException(string message, Exception innerException) : base(message, innerException)
		{
		}
	}
}
