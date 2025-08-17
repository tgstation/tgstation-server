using System;
using System.Net;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;

using Tgstation.Server.Host.Controllers;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <inheritdoc cref="IRestAuthorityInvoker{TAuthority}" />
	sealed class RestAuthorityInvoker<TAuthority> : AuthorityInvokerBase<TAuthority>, IRestAuthorityInvoker<TAuthority>
		where TAuthority : IAuthority
	{
		/// <summary>
		/// Create an <see cref="IActionResult"/> for a given successfuly<paramref name="authorityResponse"/>.
		/// </summary>
		/// <param name="controller">The <see cref="ApiController"/> to use.</param>
		/// <param name="resultTransformer">A <see cref="Func{T, TResult}"/> transforming the <typeparamref name="TResult"/> from the <paramref name="authorityResponse"/> into the <typeparamref name="TApiModel"/>.</param>
		/// <param name="authorityResponse">The <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <returns>An <see cref="IActionResult"/> for the <paramref name="authorityResponse"/>.</returns>
		/// <typeparam name="TResult">The result <see cref="Type"/> returned in the <paramref name="authorityResponse"/>.</typeparam>
		/// <typeparam name="TApiModel">The REST API result model built from <paramref name="authorityResponse"/>.</typeparam>
		static IActionResult CreateSuccessfulActionResult<TResult, TApiModel>(
			ApiController controller,
			Func<TResult, TApiModel> resultTransformer,
			AuthorityResponse<TResult> authorityResponse)
			where TApiModel : notnull
		{
			if (authorityResponse.IsNoContent!.Value)
				return controller.NoContent();

			var successResponse = authorityResponse.SuccessResponse;
			var result = resultTransformer(authorityResponse.Result!);
			return successResponse switch
			{
				HttpSuccessResponse.Ok => controller.Json(result),
				HttpSuccessResponse.Created => controller.Created(result),
				HttpSuccessResponse.Accepted => controller.Accepted(result),
				_ => throw new InvalidOperationException($"Invalid {nameof(HttpSuccessResponse)}: {successResponse}"),
			};
		}

		/// <summary>
		/// Create an <see cref="IActionResult"/> for a given <paramref name="authorityResponse"/> if it is erroring.
		/// </summary>
		/// <param name="controller">The <see cref="ApiController"/> to use.</param>
		/// <param name="authorityResponse">The <see cref="AuthorityResponse"/>.</param>
		/// <returns>An <see cref="IActionResult"/> if the <paramref name="authorityResponse"/> is not successful, <see langword="null"/> otherwise. If <see langword="null"/> is returned, <paramref name="authorityResponse"/> is not <see langword="null"/>.</returns>
		static IActionResult? CreateErroredActionResult(
			ApiController controller,
			AuthorityResponse? authorityResponse)
		{
			if (authorityResponse == null)
				return controller.Forbid();

			if (authorityResponse.Success)
				return null;

			var errorMessage = authorityResponse.ErrorMessage;
			var failureResponse = authorityResponse.FailureResponse;
			return failureResponse switch
			{
				HttpFailureResponse.BadRequest => controller.BadRequest(errorMessage),
				HttpFailureResponse.Unauthorized => controller.Unauthorized(),
				HttpFailureResponse.Forbidden => controller.Forbid(),
				HttpFailureResponse.NotFound => controller.NotFound(errorMessage),
				HttpFailureResponse.NotAcceptable => controller.StatusCode(HttpStatusCode.NotAcceptable, errorMessage),
				HttpFailureResponse.Conflict => controller.Conflict(errorMessage),
				HttpFailureResponse.Gone => controller.StatusCode(HttpStatusCode.Gone, errorMessage),
				HttpFailureResponse.UnprocessableEntity => controller.UnprocessableEntity(errorMessage),
				HttpFailureResponse.FailedDependency => controller.StatusCode(HttpStatusCode.FailedDependency, errorMessage),
				HttpFailureResponse.RateLimited => controller.StatusCode(HttpStatusCode.TooManyRequests, errorMessage),
				HttpFailureResponse.NotImplemented => controller.StatusCode(HttpStatusCode.NotImplemented, errorMessage),
				HttpFailureResponse.ServiceUnavailable => controller.StatusCode(HttpStatusCode.ServiceUnavailable, errorMessage),
				_ => throw new InvalidOperationException($"Invalid {nameof(HttpFailureResponse)}: {failureResponse}"),
			};
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RestAuthorityInvoker{TAuthority}"/> class.
		/// </summary>
		/// <param name="authority">The <typeparamref name="TAuthority"/>.</param>
		/// <param name="authorizationService">The <see cref="IAuthorizationService"/> to use.</param>
		public RestAuthorityInvoker(TAuthority authority, IAuthorizationService authorizationService)
			: base(authority, authorizationService)
		{
		}

		/// <inheritdoc />
		async ValueTask<IActionResult> IRestAuthorityInvoker<TAuthority>.Invoke(ApiController controller, Func<TAuthority, RequirementsGated<AuthorityResponse>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(controller);
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var authorityResponse = await ExecuteIfRequirementsSatisfied(requirementsGate);
			return CreateErroredActionResult(controller, authorityResponse) ?? controller.NoContent();
		}

		/// <inheritdoc />
		async ValueTask<IActionResult> IRestAuthorityInvoker<TAuthority>.Invoke<TResult, TApiModel>(ApiController controller, Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(controller);
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var authorityResponse = await ExecuteIfRequirementsSatisfied(requirementsGate);
			var erroredResult = CreateErroredActionResult(controller, authorityResponse);
			if (erroredResult != null)
				return erroredResult;

			return CreateSuccessfulActionResult(controller, result => result, authorityResponse!);
		}

		/// <inheritdoc />
		async ValueTask<IActionResult> IRestAuthorityInvoker<TAuthority>.InvokeTransformable<TResult, TApiModel>(ApiController controller, Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(controller);
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var authorityResponse = await ExecuteIfRequirementsSatisfied(requirementsGate);
			var erroredResult = CreateErroredActionResult(controller, authorityResponse);
			if (erroredResult != null)
				return erroredResult;

			return CreateSuccessfulActionResult(controller, result => result.ToApi(), authorityResponse!);
		}

		/// <inheritdoc />
		async ValueTask<IActionResult> IRestAuthorityInvoker<TAuthority>.InvokeTransformable<TResult, TApiModel, TTransformer>(
			ApiController controller,
			Func<TAuthority, RequirementsGated<AuthorityResponse<TResult>>> authorityInvoker)
		{
			ArgumentNullException.ThrowIfNull(controller);
			ArgumentNullException.ThrowIfNull(authorityInvoker);

			var requirementsGate = authorityInvoker(Authority);
			var authorityResponse = await ExecuteIfRequirementsSatisfied(requirementsGate);
			var erroredResult = CreateErroredActionResult(controller, authorityResponse);
			if (erroredResult != null)
				return erroredResult;

			return CreateSuccessfulActionResult(controller, result => new TTransformer().CompiledExpression(result), authorityResponse!);
		}

		/// <inheritdoc />
		async ValueTask<IActionResult> IRestAuthorityInvoker<TAuthority>.InvokeTransformable<TResult, TApiModel, TTransformer>(
			ApiController controller,
			Func<TAuthority, RequirementsGated<Projectable<TResult, TApiModel>>> authorityInvoker)
		{
			await Task.Yield();
			throw new NotImplementedException();
		}
	}
}
