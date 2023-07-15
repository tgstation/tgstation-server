using System;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Thrown when trying to generate <see cref="ApiHeaders"/> from <see cref="Microsoft.AspNetCore.Http.Headers.RequestHeaders"/> fails.
	/// </summary>
	public sealed class HeadersException : Exception
	{
		/// <summary>
		/// The <see cref="HeaderTypes"/>s that are missing or malformed.
		/// </summary>
		public HeaderTypes MissingOrMalformedHeaders { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="HeadersException"/> class.
		/// </summary>
		/// <param name="missingOrMalformedHeaders">The value of <see cref="MissingOrMalformedHeaders"/>.</param>
		/// <param name="message">The error message.</param>
		public HeadersException(HeaderTypes missingOrMalformedHeaders, string message)
			: base(message)
		{
			MissingOrMalformedHeaders = missingOrMalformedHeaders;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HeadersException"/> class.
		/// </summary>
		public HeadersException()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HeadersException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		public HeadersException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="HeadersException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="innerException">The inner <see cref="Exception"/> for the base <see cref="Exception"/>.</param>
		public HeadersException(string message, Exception innerException)
			: base(message, innerException)
		{
		}
	}
}
