using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client makes a request while the server is starting or stopping
	/// </summary>
	public sealed class ServiceUnavailableException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="ServiceUnavailableException"/>
		/// </summary>
		public ServiceUnavailableException() : base(new ErrorMessage
		{
			Message = "The server is currently starting or stopping and is unable to process the request!",
			SeverApiVersion = null
		}, HttpStatusCode.ServiceUnavailable)
		{ }

		/// <summary>
		/// Construct an <see cref="ServiceUnavailableException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public ServiceUnavailableException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="ServiceUnavailableException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public ServiceUnavailableException(string message, Exception innerException) : base(message, innerException) { }
	}
}