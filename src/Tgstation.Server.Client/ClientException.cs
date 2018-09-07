using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Exceptions thrown by <see cref="IServerClient"/>s
	/// </summary>
	public abstract class ClientException : Exception
	{
		/// <summary>
		/// The <see cref="HttpStatusCode"/> of the <see cref="ClientException"/>
		/// </summary>
		public HttpStatusCode StatusCode { get; }

		/// <summary>
		/// The <see cref="Version"/> of the server's API
		/// </summary>
		public Version ServerApiVersion { get; }

		/// <summary>
		/// Construct a <see cref="ClientException"/> using an <paramref name="errorMessage"/> and <paramref name="statusCode"/>
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> associated with the <see cref="ClientException"/></param>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/> of the <see cref="ClientException"/></param>
		protected ClientException(ErrorMessage errorMessage, HttpStatusCode statusCode) : base(errorMessage == null ? throw new ArgumentNullException(nameof(errorMessage)) : errorMessage.Message ?? "Unknown Error")
		{
			StatusCode = statusCode;
			ServerApiVersion = errorMessage?.SeverApiVersion;
		}

		/// <summary>
		/// Construct a <see cref="ClientException"/>
		/// </summary>
		protected ClientException() { }

		/// <summary>
		/// Construct a <see cref="ClientException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		protected ClientException(string message) : base(message) { }

		/// <summary>
		/// Construct a <see cref="ClientException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		protected ClientException(string message, Exception innerException) : base(message, innerException) { }
	}
}
