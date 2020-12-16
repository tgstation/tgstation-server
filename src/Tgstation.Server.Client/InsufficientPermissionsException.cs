using System;
using System.Net.Http;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when the client attempts to perform an action they do not have the rights for
	/// </summary>
	public sealed class InsufficientPermissionsException : ApiException
	{
		/// <summary>
		/// Construct an <see cref="InsufficientPermissionsException"/>
		/// </summary>
		/// <param name="responseMessage">The <see cref="HttpResponseMessage"/> for the <see cref="ClientException"/>.</param>
		public InsufficientPermissionsException(HttpResponseMessage responseMessage) : base(
			responseMessage,
			"The current user has insufficient permissions to perform the requested operation!")
		{ }

		/// <summary>
		/// Intializes a new instance of the <see cref="InsufficientPermissionsException"/> <see langword="class"/>.
		/// </summary>
		public InsufficientPermissionsException() { }

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
