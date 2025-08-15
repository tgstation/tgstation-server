using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using Microsoft.AspNetCore.Mvc;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// An <see cref="AuthorityResponse"/> with a <see cref="Result"/>.
	/// </summary>
	/// <typeparam name="TResult">The <see cref="Type"/> of success response.</typeparam>
	public sealed class AuthorityResponse<TResult> : AuthorityResponse
	{
		/// <summary>
		/// An expression to convert a <typeparamref name="TResult"/> into an <see cref="AuthorityResponse{TResult}"/>. The resulting <see cref="AuthorityResponse{TResult}"/> MUST NOT be used.
		/// </summary>
		public static Expression<Func<TResult, AuthorityResponse<TResult>>> MappingExpression { get; }

		/// <inheritdoc />
		[MemberNotNullWhen(true, nameof(IsNoContent))]
		public override bool Success => base.Success;

		/// <summary>
		/// Checks if a the <see cref="AuthorityResponse{TResult}"/> is a no content result. Only set on <see cref="Success"/>.
		/// </summary>
		[MemberNotNullWhen(false, nameof(Result))]
		[MemberNotNullWhen(false, nameof(Result))]
		public bool? IsNoContent => Success ? Result == null : null;

		/// <summary>
		/// The success <typeparamref name="TResult"/>.
		/// </summary>
		public TResult? Result { get; private init; }

		/// <summary>
		/// The <see cref="HttpSuccessResponse"/> for generating the <see cref="IActionResult"/>s.
		/// </summary>
		public HttpSuccessResponse? SuccessResponse { get; }

		/// <summary>
		/// Initializes static members of the <see cref="AuthorityResponse{TResult}"/> class.
		/// </summary>
		static AuthorityResponse()
		{
			MappingExpression = result => new AuthorityResponse<TResult>
			{
				Result = result,
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityResponse{TResult}"/> class.
		/// </summary>
		/// <param name="errorMessage">The <see cref="ErrorMessageResponse"/>.</param>
		/// <param name="httpResponse">The <see cref="HttpFailureResponse"/>.</param>
		public AuthorityResponse(ErrorMessageResponse errorMessage, HttpFailureResponse httpResponse)
			: base(errorMessage, httpResponse)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityResponse{TResult}"/> class.
		/// </summary>
		/// <param name="result">The value of <see cref="Result"/>.</param>
		/// <param name="httpResponse">The value of <see cref="SuccessResponse"/>.</param>
		public AuthorityResponse(TResult result, HttpSuccessResponse httpResponse = HttpSuccessResponse.Ok)
		{
			Result = result ?? throw new ArgumentNullException(nameof(result));
			SuccessResponse = httpResponse;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityResponse{TResult}"/> class.
		/// </summary>
		/// <remarks>This generates an HTTP 204 response.</remarks>
		public AuthorityResponse()
		{
		}
	}
}
