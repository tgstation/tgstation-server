using System;
using System.Net.Http;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when an error occurs in the server
	/// </summary>
	public sealed class ServerErrorException : ApiException
	{
		/// <summary>
		/// Construct an <see cref="ServerErrorException"/>
		/// </summary>
		public ServerErrorException() { }

		/// <summary>
		/// Construct an <see cref="ServerErrorException"/> with a given <paramref name="errorMessage"/>
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/> for the <see cref="ApiException"/></param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/></param>
		public ServerErrorException(ErrorMessageResponse? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerErrorException"/> <see langword="class"/>.
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/>.</param>
		public ServerErrorException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="ServerErrorException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public ServerErrorException(string message, Exception innerException) : base(message, innerException) { }
	}
}
