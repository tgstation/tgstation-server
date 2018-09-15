using System;
using System.Globalization;
using System.Net;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Client
{
	sealed class UnrecognizedResponseException : ClientException
	{
		/// <summary>
		/// Construct an <see cref="UnrecognizedResponseException"/> with the <paramref name="data"/> of a response body and the <paramref name="statusCode"/>
		/// </summary>
		/// <param name="data">The body of the response</param>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/> for the <see cref="ClientException"/></param>
		public UnrecognizedResponseException(string data, HttpStatusCode statusCode) : base(new ErrorMessage
		{
			Message = String.Format(CultureInfo.InvariantCulture, "Unrecognized response body: {0}", data),
			SeverApiVersion = null
		}, statusCode)
		{ }

		/// <summary>
		/// Construct a <see cref="UnrecognizedResponseException"/>
		/// </summary>
		public UnrecognizedResponseException() { }

		/// <summary>
		/// Construct an <see cref="UnrecognizedResponseException"/> with a <paramref name="message"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		public UnrecognizedResponseException(string message) : base(message) { }

		/// <summary>
		/// Construct an <see cref="UnrecognizedResponseException"/> with a <paramref name="message"/> and <paramref name="innerException"/>
		/// </summary>
		/// <param name="message">The message for the <see cref="Exception"/></param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/></param>
		public UnrecognizedResponseException(string message, Exception innerException) : base(message, innerException) { }
	}
}
