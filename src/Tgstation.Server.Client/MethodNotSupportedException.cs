using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client tries to use a currently unsupported API
	/// </summary>
	public sealed class MethodNotSupportedException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="MethodNotSupportedException"/>
		/// </summary>
		public MethodNotSupportedException() : base(new ErrorMessage
		{
			Message = "This method is not currently supported!",
			SeverApiVersion = null
		}, HttpStatusCode.NotImplemented)
		{ }

		/// <summary>
		/// Construct an <see cref="MethodNotSupportedException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public MethodNotSupportedException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="MethodNotSupportedException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public MethodNotSupportedException(string message, Exception innerException) : base(message, innerException) { }
	}
}