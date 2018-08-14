using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client provides invalid credentials
	/// </summary>
	public sealed class RequestTimeoutException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="RequestTimeoutException"/>
		/// </summary>
		public RequestTimeoutException() : base(new ErrorMessage
		{
			Message = "The request timed out!",
			SeverApiVersion = null
		}, HttpStatusCode.RequestTimeout)
		{ }

		/// <summary>
		/// Construct an <see cref="RequestTimeoutException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public RequestTimeoutException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="RequestTimeoutException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public RequestTimeoutException(string message, Exception innerException) : base(message, innerException) { }
	}
}