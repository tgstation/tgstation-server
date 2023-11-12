using System;

namespace Tgstation.Server.Api
{
	/// <summary>
	/// Thrown when trying to generate <see cref="ApiHeaders"/> from <see cref="Microsoft.AspNetCore.Http.Headers.RequestHeaders"/> fails.
	/// </summary>
	public sealed class HeadersException : Exception
	{
		/// <summary>
		/// The <see cref="HeaderErrorTypes"/>s that are missing or malformed.
		/// </summary>
		public HeaderErrorTypes ParseErrors { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="HeadersException"/> class.
		/// </summary>
		/// <param name="parseErrors">The value of <see cref="ParseErrors"/>.</param>
		/// <param name="message">The error message.</param>
		public HeadersException(HeaderErrorTypes parseErrors, string message)
			: base(message)
		{
			ParseErrors = parseErrors;
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
