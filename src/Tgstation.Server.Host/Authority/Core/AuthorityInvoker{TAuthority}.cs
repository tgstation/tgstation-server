using System;
using System.Net;
using System.Threading.Tasks;

using HotChocolate.Execution;

using Microsoft.AspNetCore.Mvc;
using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.GraphQL;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// Invokes <typeparamref name="TAuthority"/>s.
	/// </summary>
	/// <typeparam name="TAuthority">The <see cref="IAuthority"/> invoked.</typeparam>
	sealed class AuthorityInvoker<TAuthority> : IRestAuthorityInvoker<TAuthority>, IGraphQLAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// The <see cref="IAuthority"/> being invoked.
		/// </summary>
		readonly TAuthority authority;

		/// <summary>
		/// Throws a <see cref="QueryException"/> for errored <paramref name="authorityResponse"/>s.
		/// </summary>
		/// <param name="authorityResponse">The potentially errored <paramref name="authorityResponse"/>.</param>
		static void ThrowGraphQLErrorIfNecessary(AuthorityResponse authorityResponse)
		{
			if (authorityResponse.Success)
				return;

			var fallbackString = authorityResponse.FailureResponse.ToString()!;
			throw new ErrorMessageException(authorityResponse.ErrorMessage, fallbackString);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="AuthorityInvoker{TAuthority}"/> class.
		/// </summary>
		/// <param name="authority">The value of <see cref="authority"/>.</param>
		public AuthorityInvoker(TAuthority authority)
		{
			this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
		}

		/// <inheritdoc />
		public async ValueTask<IActionResult> Invoke(ApiController controller, Func<TAuthority, ValueTask<AuthorityResponse>> authorityInvoker)
		{
			var authorityResponse = await authorityInvoker(authority);
			return CreateErroredActionResult(controller, authorityResponse) ?? controller.NoContent();
		}

		/// <inheritdoc />
		public async ValueTask<IActionResult> InvokeTransformable<TResult, TApiModel>(ApiController controller, Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)

			where TResult : notnull, IApiTransformable<TApiModel>
			where TApiModel : notnull
		{
			var authorityResponse = await authorityInvoker(authority);
			var erroredResult = CreateErroredActionResult(controller, authorityResponse);
			if (erroredResult != null)
				return erroredResult;

			var result = authorityResponse.Result!;
			var apiModel = result.ToApi();
			return CreateSuccessfulActionResult(controller, apiModel, authorityResponse);
		}

		/// <inheritdoc />
		async ValueTask<IActionResult> IRestAuthorityInvoker<TAuthority>.Invoke<TResult, TApiModel>(ApiController controller, Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
		{
			var authorityResponse = await authorityInvoker(authority);
			var erroredResult = CreateErroredActionResult(controller, authorityResponse);
			if (erroredResult != null)
				return erroredResult;

			var result = authorityResponse.Result!;
			return CreateSuccessfulActionResult(controller, result, authorityResponse);
		}

		/// <inheritdoc />
		async ValueTask IGraphQLAuthorityInvoker<TAuthority>.Invoke(Func<TAuthority, ValueTask<AuthorityResponse>> authorityInvoker)
		{
			var authorityResponse = await authorityInvoker(authority);
			ThrowGraphQLErrorIfNecessary(authorityResponse);
		}

		/// <inheritdoc />
		public async ValueTask<TApiModel> Invoke<TResult, TApiModel>(Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
			where TResult : TApiModel
			where TApiModel : notnull
		{
			var authorityResponse = await authorityInvoker(authority);
			ThrowGraphQLErrorIfNecessary(authorityResponse);
			return authorityResponse.Result!;
		}

		/// <inheritdoc />
		async ValueTask<TApiModel> IGraphQLAuthorityInvoker<TAuthority>.InvokeTransformable<TResult, TApiModel>(Func<TAuthority, ValueTask<AuthorityResponse<TResult>>> authorityInvoker)
		{
			var authorityResponse = await authorityInvoker(authority);
			ThrowGraphQLErrorIfNecessary(authorityResponse);
			return authorityResponse.Result!.ToApi();
		}

		/// <summary>
		/// Create an <see cref="IActionResult"/> for a given <paramref name="authorityResponse"/> if it is erroring.
		/// </summary>
		/// <param name="controller">The <see cref="ApiController"/> to use.</param>
		/// <param name="authorityResponse">The <see cref="AuthorityResponse"/>.</param>
		/// <returns>An <see cref="IActionResult"/> if the <paramref name="authorityResponse"/> is not successful, <see langword="null"/> otherwise.</returns>
		IActionResult? CreateErroredActionResult(ApiController controller, AuthorityResponse authorityResponse)
		{
			if (authorityResponse.Success)
				return null;

			var errorMessage = authorityResponse.ErrorMessage;
			var failureResponse = authorityResponse.FailureResponse;
			return failureResponse switch
			{
				HttpFailureResponse.BadRequest => controller.BadRequest(errorMessage),
				HttpFailureResponse.Unauthorized => controller.Unauthorized(errorMessage),
				HttpFailureResponse.Forbidden => controller.Forbid(),
				HttpFailureResponse.NotFound => controller.NotFound(errorMessage),
				HttpFailureResponse.NotAcceptable => controller.StatusCode(HttpStatusCode.NotAcceptable, errorMessage),
				HttpFailureResponse.Conflict => controller.Conflict(errorMessage),
				HttpFailureResponse.Gone => controller.StatusCode(HttpStatusCode.Gone, errorMessage),
				HttpFailureResponse.UnprocessableEntity => controller.UnprocessableEntity(errorMessage),
				HttpFailureResponse.FailedDependency => controller.StatusCode(HttpStatusCode.FailedDependency, errorMessage),
				HttpFailureResponse.RateLimited => controller.StatusCode(HttpStatusCode.TooManyRequests, errorMessage),
				HttpFailureResponse.NotImplemented => controller.StatusCode(HttpStatusCode.NotImplemented, errorMessage),
				_ => throw new InvalidOperationException($"Invalid {nameof(HttpFailureResponse)}: {failureResponse}"),
			};
		}

		/// <summary>
		/// Create an <see cref="IActionResult"/> for a given successfuly <paramref name="authorityResponse"/> API <paramref name="result"/>.
		/// </summary>
		/// <param name="controller">The <see cref="ApiController"/> to use.</param>
		/// <param name="result">The resulting <typeparamref name="TApiModel"/> from the <paramref name="authorityResponse"/>.</param>
		/// <param name="authorityResponse">The <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <returns>An <see cref="IActionResult"/> for the <paramref name="authorityResponse"/>.</returns>
		/// <typeparam name="TResult">The result <see cref="Type"/> returned in the <paramref name="authorityResponse"/>.</typeparam>
		/// <typeparam name="TApiModel">The REST API result model built from <paramref name="authorityResponse"/>.</typeparam>
		IActionResult CreateSuccessfulActionResult<TResult, TApiModel>(ApiController controller, TApiModel result, AuthorityResponse<TResult> authorityResponse)
			where TApiModel : notnull
		{
			var successResponse = authorityResponse.SuccessResponse;
			return successResponse switch
			{
				HttpSuccessResponse.Ok => controller.Json(result),
				HttpSuccessResponse.Created => controller.Created(result),
				HttpSuccessResponse.Accepted => controller.Accepted(result),
				_ => throw new InvalidOperationException($"Invalid {nameof(HttpSuccessResponse)}: {successResponse}"),
			};
		}
	}
}
