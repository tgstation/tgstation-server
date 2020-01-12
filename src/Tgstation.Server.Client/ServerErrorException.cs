using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when an error occurs in the server
	/// </summary>
	public sealed class ServerErrorException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="ServerErrorException"/>
		/// </summary>
		public ServerErrorException() { }

		/// <summary>
		/// Construct an <see cref="ServerErrorException"/> with a given <paramref name="errorMessage"/>
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> for the <see cref="ClientException"/></param>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/> for the <see cref="ClientException"/></param>
		public ServerErrorException(ErrorMessage errorMessage, HttpStatusCode statusCode) : base(errorMessage, statusCode)
		{
		}

		/// <summary>
		/// Construct an <see cref="ServerErrorException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public ServerErrorException(string message, Exception innerException) : base(message, innerException) { }
	}
}