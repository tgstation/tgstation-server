using System;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client attempts to perform an action they do not have the rights for
	/// </summary>
	public sealed class InsufficientPermissionsException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="InsufficientPermissionsException"/>
		/// </summary>
		public InsufficientPermissionsException() : base(new ErrorMessage
		{
			Message = "The credentials provided do not have sufficient rights to make this request!",
			SeverApiVersion = null
		}, HttpStatusCode.Forbidden)
		{ }

		/// <summary>
		/// Construct an <see cref="InsufficientPermissionsException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public InsufficientPermissionsException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="InsufficientPermissionsException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public InsufficientPermissionsException(string message, Exception innerException) : base(message, innerException) { }
	}
}