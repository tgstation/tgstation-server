using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the API version of the client is not compatible with the server's
	/// </summary>
	public sealed class ApiMismatchException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="ApiMismatchException"/> using an <paramref name="errorMessage"/>
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> for the <see cref="ClientException"/></param>
		public ApiMismatchException(ErrorMessage errorMessage) : base(errorMessage, HttpStatusCode.BadRequest)
		{
			if (errorMessage == null)
				throw new ArgumentNullException(nameof(errorMessage));
		}

		/// <summary>
		/// Construct an <see cref="ApiMismatchException"/>
		/// </summary>
		public ApiMismatchException() { }

		/// <summary>
		/// Construct an <see cref="ApiMismatchException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public ApiMismatchException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="ApiMismatchException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public ApiMismatchException(string message, Exception innerException) : base(message, innerException) { }
	}
}