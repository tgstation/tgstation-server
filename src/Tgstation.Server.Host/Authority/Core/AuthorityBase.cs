using System;
using System.Globalization;

using Microsoft.Extensions.Logging;

using Octokit;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// Base implementation of <see cref="IAuthority"/>.
	/// </summary>
	abstract class AuthorityBase : IAuthority
	{
		/// <summary>
		/// Gets the <see cref="IAuthenticationContext"/> for the <see cref="AuthorityBase"/>.
		/// </summary>
		protected IAuthenticationContext AuthenticationContext { get; }

		/// <summary>
		/// Gets the <see cref="ILogger"/> for the <see cref="AuthorityBase"/>.
		/// </summary>
		protected ILogger<AuthorityBase> Logger { get; }

		/// <summary>
		/// Generates a <see cref="HttpFailureResponse.BadRequest"/> type <see cref="AuthorityResponse{TResult}"/>.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> of the <see cref="AuthorityResponse{TResult}.Result"/>.</typeparam>
		/// <param name="errorCode">The <see cref="ErrorCode"/>.</param>
		/// <returns>A new, errored <see cref="AuthorityResponse{TResult}"/>.</returns>
		protected static AuthorityResponse<TResult> BadRequest<TResult>(ErrorCode errorCode)
			=> new(
				new ErrorMessageResponse(errorCode),
				HttpFailureResponse.BadRequest);

		/// <summary>
		/// Generates a <see cref="HttpFailureResponse.Unauthorized"/> type <see cref="AuthorityResponse{TResult}"/>.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> of the <see cref="AuthorityResponse{TResult}.Result"/>.</typeparam>
		/// <returns>A new, errored <see cref="AuthorityResponse{TResult}"/>.</returns>
		protected static AuthorityResponse<TResult> Unauthorized<TResult>()
			=> new(
				new ErrorMessageResponse(),
				HttpFailureResponse.Unauthorized);

		/// <summary>
		/// Generates a <see cref="HttpFailureResponse.Forbidden"/> type <see cref="AuthorityResponse{TResult}"/>.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> of the <see cref="AuthorityResponse{TResult}.Result"/>.</typeparam>
		/// <returns>A new, errored <see cref="AuthorityResponse{TResult}"/>.</returns>
		protected static AuthorityResponse<TResult> Forbid<TResult>()
			=> new(
				new ErrorMessageResponse(),
				HttpFailureResponse.Forbidden);

		/// <summary>
		/// Generates a <see cref="HttpFailureResponse.NotFound"/> type <see cref="AuthorityResponse{TResult}"/>.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> of the <see cref="AuthorityResponse{TResult}.Result"/>.</typeparam>
		/// <returns>A new, errored <see cref="AuthorityResponse{TResult}"/>.</returns>
		protected static AuthorityResponse<TResult> NotFound<TResult>()
			=> new(
				new ErrorMessageResponse(),
				HttpFailureResponse.NotFound);

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityBase"/> class.
		/// </summary>
		/// <param name="authenticationContext">The value of <see cref="AuthenticationContext"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		protected AuthorityBase(
			IAuthenticationContext authenticationContext,
			ILogger<AuthorityBase> logger)
		{
			AuthenticationContext = authenticationContext ?? throw new ArgumentNullException(nameof(authenticationContext));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <summary>
		/// Generates a <see cref="HttpFailureResponse.RateLimited"/> type <see cref="AuthorityResponse"/>.
		/// </summary>
		/// <typeparam name="TResult">The <see cref="Type"/> of the <see cref="AuthorityResponse{TResult}.Result"/>.</typeparam>
		/// <param name="rateLimitException">The thrown <see cref="RateLimitExceededException"/>.</param>
		/// <returns>A new, errored <see cref="AuthorityResponse{TResult}"/>.</returns>
		protected AuthorityResponse<TResult> RateLimit<TResult>(RateLimitExceededException rateLimitException)
		{
			Logger.LogWarning(rateLimitException, "Exceeded GitHub rate limit!");
			var secondsString = Math.Ceiling(rateLimitException.GetRetryAfterTimeSpan().TotalSeconds).ToString(CultureInfo.InvariantCulture);
			return new(
				new ErrorMessageResponse(ErrorCode.GitHubApiRateLimit)
				{
					AdditionalData = $"Retry-After: {secondsString}s",
				},
				HttpFailureResponse.RateLimited);
		}
	}
}
