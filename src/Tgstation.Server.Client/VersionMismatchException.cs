using System;
using System.Net.Http;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the API version of the client is not compatible with the server's.
	/// </summary>
	public sealed class VersionMismatchException : ApiException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VersionMismatchException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> for the <see cref="ApiException"/>.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public VersionMismatchException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage)
			: base(errorMessage, responseMessage)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VersionMismatchException"/> class.
		/// </summary>
		public VersionMismatchException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VersionMismatchException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public VersionMismatchException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="VersionMismatchException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public VersionMismatchException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
