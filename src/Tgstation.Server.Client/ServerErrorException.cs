using System;
using System.Net;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// Occurs when an error occurs in the server
	/// </summary>
	public sealed class ServerErrorException : ClientException
	{
		/// <summary>
		/// The raw HTML of the error
		/// </summary>
		public string Html { get; }

		/// <summary>
		/// Construct an <see cref="ServerErrorException"/>
		/// </summary>
		public ServerErrorException() { }

		/// <summary>
		/// Construct an <see cref="ServerErrorException"/> with <paramref name="html"/>
		/// </summary>
		/// <param name="html">The raw HTML response of the <see cref="ServerErrorException"/></param>
		public ServerErrorException(string html) : base(null, HttpStatusCode.InternalServerError)
		{
			Html = html;
		}

		/// <summary>
		/// Construct an <see cref="ServerErrorException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public ServerErrorException(string message, Exception innerException) : base(message, innerException) { }
	}
}