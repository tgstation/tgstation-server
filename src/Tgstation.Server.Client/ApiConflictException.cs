using System;
using System.Net.Http;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the server returns a bad request response if the <see cref="ApiException.ErrorCode"/> is present. The server returned an unknown reponse otherwise.
	/// </summary>
	public sealed class ApiConflictException : ApiException
	{
		/// <summary>
		/// Construct an <see cref="ApiConflictException"/> using an <paramref name="errorMessage"/>
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> for the <see cref="ApiException"/></param>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/></param>
		public ApiConflictException(ErrorMessage? errorMessage, HttpResponseMessage responseMessage) : base(errorMessage, responseMessage)
		{ }

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