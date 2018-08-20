using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the server returns an unknown response
	/// </summary>
	public sealed class ApiConflictException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="ApiConflictException"/> using an <paramref name="errorMessage"/>
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> for the <see cref="ClientException"/></param>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/> for the <see cref="ClientException"/></param>
		public ApiConflictException(ErrorMessage errorMessage, HttpStatusCode statusCode) : base(errorMessage ?? new ErrorMessage
		{
			Message = "An unknown API error occurred!",
			SeverApiVersion = null
		}, statusCode) { }

		/// <summary>
		/// Construct an <see cref="ApiConflictException"/>
		/// </summary>
		public ApiConflictException() { }

		/// <summary>
		/// Construct an <see cref="ApiConflictException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public ApiConflictException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="ApiConflictException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public ApiConflictException(string message, Exception innerException) : base(message, innerException) { }
	}
}