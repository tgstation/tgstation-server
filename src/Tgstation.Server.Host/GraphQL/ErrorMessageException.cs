using System;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

#pragma warning disable CA1032 // Shitty unneeded additional Exception constructors

namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// <see cref="Exception"/> representing <see cref="ErrorMessageResponse"/>s.
	/// </summary>
	public sealed class ErrorMessageException : Exception
	{
		/// <summary>
		/// The <see cref="ErrorMessageResponse.ErrorCode"/>.
		/// </summary>
		public ErrorCode? ErrorCode { get; }

		/// <summary>
		/// The <see cref="ErrorMessageResponse.AdditionalData"/>.
		/// </summary>
		public string? AdditionalData { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessageException"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/>.</param>
		/// <param name="fallbackMessage">Fallback <see cref="Exception.Message"/>.</param>
		public ErrorMessageException(ErrorMessageResponse errorMessage, string fallbackMessage)
			: base((errorMessage ?? throw new ArgumentNullException(nameof(errorMessage))).Message ?? fallbackMessage)
		{
			ErrorCode = errorMessage.ErrorCode != default ? errorMessage.ErrorCode : null;
			AdditionalData = errorMessage.AdditionalData;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ErrorMessageException"/> class.
		/// </summary>
		/// <param name="errorCode">The <see cref="ErrorCode"/>.</param>
		public ErrorMessageException(ErrorCode errorCode)
			: this(new ErrorMessageResponse(errorCode), String.Empty)
		{
		}
	}
}
