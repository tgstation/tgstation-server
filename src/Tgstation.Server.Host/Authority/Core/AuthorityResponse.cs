using System;
using System.Diagnostics.CodeAnalysis;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// Represents a response from an authority.
	/// </summary>
	public class AuthorityResponse
	{
		/// <summary>
		/// Checks if the <see cref="AuthorityResponse"/> was successful.
		/// </summary>
		[MemberNotNullWhen(false, nameof(ErrorMessage))]
		[MemberNotNullWhen(false, nameof(FailureResponse))]
		public virtual bool Success => ErrorMessage == null;

		/// <summary>
		/// Gets the associated <see cref="ErrorMessageResponse"/>. Must only be used if <see cref="Success"/> is <see langword="false"/>.
		/// </summary>
		public ErrorMessageResponse? ErrorMessage { get; }

		/// <summary>
		/// The <see cref="HttpFailureResponse"/>.
		/// </summary>
		public HttpFailureResponse? FailureResponse { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityResponse"/> class.
		/// </summary>
		public AuthorityResponse()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityResponse"/> class.
		/// </summary>
		/// <param name="errorMessage">The value of <see cref="ErrorMessage"/>.</param>
		/// <param name="failureResponse">The value of <see cref="FailureResponse"/>.</param>
		public AuthorityResponse(ErrorMessageResponse errorMessage, HttpFailureResponse failureResponse)
		{
			ErrorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage));
			FailureResponse = failureResponse;
		}
	}
}
