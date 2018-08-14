using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client performs an action that would result in data conflict
	/// </summary>
	public sealed class ConflictException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="ConflictException"/> with a <paramref name="errorMessage"/> and <paramref name="statusCode"/>
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessage"/> for the <see cref="ClientException"/></param>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/> for the <see cref="ClientException"/></param>
		public ConflictException(ErrorMessage errorMessage, HttpStatusCode statusCode) : base(errorMessage, statusCode)
		{ }
		
		/// <summary>
		/// Construct a <see cref="ConflictException"/>
		/// </summary>
		public ConflictException() { }

		/// <summary>
		/// Construct an <see cref="ConflictException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public ConflictException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="ConflictException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public ConflictException(string message, Exception innerException) : base(message, innerException) { }
	}
}