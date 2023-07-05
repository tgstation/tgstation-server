using System;
using System.Net.Http;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// API related <see cref="Exception"/>s.
	/// </summary>
	public abstract class ApiException : ClientException
	{
		/// <summary>
		/// The <see cref="Version"/> of the server's API.
		/// </summary>
		public Version? ServerApiVersion { get; }

		/// <summary>
		/// Additional error data from the server.
		/// </summary>
		public string? AdditionalServerData { get; }

		/// <summary>
		/// The API <see cref="ErrorCode"/> if applicable.
		/// </summary>
		public ErrorCode? ErrorCode { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> returned from the API.</param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/>.</param>
		protected ApiException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage)
			: base(
				responseMessage,
				errorMessage?.Message ?? $"HTTP {responseMessage?.StatusCode ?? throw new ArgumentNullException(nameof(responseMessage))}. Unknown API error, ErrorMessage payload not present!")
		{
			ServerApiVersion = errorMessage?.ServerApiVersion;
			AdditionalServerData = errorMessage?.AdditionalData;
			ErrorCode = errorMessage?.ErrorCode;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiException"/> class.
		/// </summary>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/>.</param>
		/// <param name="message">The <see cref="Exception.Message"/>.</param>
		protected ApiException(HttpResponseMessage responseMessage, string message)
			: base(responseMessage, message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiException"/> class.
		/// </summary>
		protected ApiException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		protected ApiException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiException"/> class.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		protected ApiException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
