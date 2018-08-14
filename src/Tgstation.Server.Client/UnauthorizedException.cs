using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client provides invalid credentials
	/// </summary>
	public sealed class UnauthorizedException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="UnauthorizedException"/>
		/// </summary>
		public UnauthorizedException() : base(new ErrorMessage
		{
			Message = "Invalid credentials!",
			SeverApiVersion = null
		}, HttpStatusCode.Unauthorized)
		{ }

		/// <summary>
		/// Construct an <see cref="UnauthorizedException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public UnauthorizedException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="UnauthorizedException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public UnauthorizedException(string message, Exception innerException) : base(message, innerException) { }
	}
}