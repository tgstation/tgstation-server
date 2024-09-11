using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

using Octokit;

using Serilog.Context;

using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Common.Extensions;
using Tgstation.Server.Host.Controllers.Results;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Extensions;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Base <see cref="Controller"/> for API functions.
	/// </summary>
	public abstract class ApiController : ApiControllerBase
	{
		/// <summary>
		/// Default size of <see cref="Paginated{TModel}"/> results.
		/// </summary>
		private const ushort DefaultPageSize = 10;

		/// <summary>
		/// Maximum size of <see cref="Paginated{TModel}"/> results.
		/// </summary>
		private const ushort MaximumPageSize = 100;

		/// <summary>
		/// The <see cref="Api.ApiHeaders"/> for the operation.
		/// </summary>
		protected ApiHeaders? ApiHeaders => ApiHeadersProvider.ApiHeaders;

		/// <summary>
		/// The <see cref="IApiHeadersProvider"/> containing value of <see cref="ApiHeaders"/>.
		/// </summary>
		protected IApiHeadersProvider ApiHeadersProvider { get; }

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the operation.
		/// </summary>
		protected IDatabaseContext DatabaseContext { get; }

		/// <summary>
		/// The <see cref="IAuthenticationContext"/> for the operation.
		/// </summary>
		protected IAuthenticationContext AuthenticationContext { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ApiController"/>.
		/// </summary>
		protected ILogger<ApiController> Logger { get; }

		/// <summary>
		/// The <see cref="Instance"/> for the operation.
		/// </summary>
		protected Models.Instance? Instance { get; }

		/// <summary>
		/// If <see cref="ApiHeaders"/> are required.
		/// </summary>
		readonly bool requireHeaders;

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiController"/> class.
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="DatabaseContext"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ApiController"/>.</param>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="apiHeadersProvider">The value of <see cref="ApiHeadersProvider"/>..</param>
		/// <param name="requireHeaders">The value of <see cref="requireHeaders"/>.</param>
		protected ApiController(
			IDatabaseContext databaseContext,
			IAuthenticationContext authenticationContext,
			IApiHeadersProvider apiHeadersProvider,
			ILogger<ApiController> logger,
			bool requireHeaders)
		{
			DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			AuthenticationContext = authenticationContext ?? throw new ArgumentNullException(nameof(authenticationContext));
			ApiHeadersProvider = apiHeadersProvider ?? throw new ArgumentNullException(nameof(apiHeadersProvider));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));

			Instance = AuthenticationContext.InstancePermissionSet?.Instance;
			this.requireHeaders = requireHeaders;
		}

		/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
		protected override async ValueTask<IActionResult?> HookExecuteAction(Func<Task> executeAction, CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(executeAction);

			// validate the headers
			if (ApiHeaders == null)
			{
				if (requireHeaders)
					return HeadersIssue(ApiHeadersProvider.HeadersException!);
			}

			var errorCase = await ValidateRequest(cancellationToken);
			if (errorCase != null)
				return errorCase;

			if (ModelState?.IsValid == false)
			{
				var errorMessages = ModelState
					.SelectMany(x => x.Value!.Errors)
					.Select(x => x.ErrorMessage)

					// We use RequiredAttributes purely for preventing properties from becoming nullable in the databases
					// We validate missing required fields in controllers
					// Unfortunately, we can't remove the whole validator for that as it checks other things like StringLength
					// This is the best way to deal with it unfortunately
					.Where(x => !x.EndsWith(" field is required.", StringComparison.Ordinal));

				if (errorMessages.Any())
					return BadRequest(
						new ErrorMessageResponse(ErrorCode.ModelValidationFailure)
						{
							AdditionalData = String.Join(Environment.NewLine, errorMessages),
						});

				ModelState.Clear();
			}

			using (ApiHeaders?.InstanceId != null
				? LogContext.PushProperty(SerilogContextHelper.InstanceIdContextProperty, ApiHeaders.InstanceId)
				: null)
			using (AuthenticationContext.Valid
				? LogContext.PushProperty(SerilogContextHelper.UserIdContextProperty, AuthenticationContext.User.Id)
				: null)
			using (LogContext.PushProperty(SerilogContextHelper.RequestPathContextProperty, $"{Request.Method} {Request.Path}"))
			{
				if (ApiHeaders != null)
				{
					var isGet = HttpMethods.IsGet(Request.Method);
					Logger.Log(
						isGet
							? LogLevel.Trace
							: LogLevel.Debug,
						"Starting API request: Version: {clientApiVersion}. {userAgentHeaderName}: {clientUserAgent}",
						ApiHeaders.ApiVersion.Semver(),
						HeaderNames.UserAgent,
						ApiHeaders.RawUserAgent);
				}
				else if (Request.Headers.TryGetValue(HeaderNames.UserAgent, out var userAgents))
					Logger.LogDebug(
						"Starting unauthorized API request. {userAgentHeaderName}: {allUserAgents}",
						HeaderNames.UserAgent,
						userAgents);
				else
					Logger.LogDebug(
						"Starting unauthorized API request. No {userAgentHeaderName}!",
						HeaderNames.UserAgent);

				await executeAction();
			}

			return null;
		}
#pragma warning restore CA1506

		/// <summary>
		/// Generic 404 response.
		/// </summary>
		/// <returns>A <see cref="NotFoundObjectResult"/> with an appropriate <see cref="ErrorMessageResponse"/>.</returns>
		protected new NotFoundObjectResult NotFound() => NotFound(new ErrorMessageResponse(ErrorCode.ResourceNeverPresent));

		/// <summary>
		/// Generic 501 response.
		/// </summary>
		/// <param name="ex">The <see cref="NotImplementedException"/> that was thrown.</param>
		/// <returns>An <see cref="ObjectResult"/> with <see cref="HttpStatusCode.NotImplemented"/>.</returns>
		protected ObjectResult RequiresPosixSystemIdentity(NotImplementedException ex)
		{
			Logger.LogTrace(ex, "System identities not implemented!");
			return this.StatusCode(HttpStatusCode.NotImplemented, new ErrorMessageResponse(ErrorCode.RequiresPosixSystemIdentity));
		}

		/// <summary>
		/// Strongly type calls to <see cref="ControllerBase.StatusCode(int)"/>.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
		/// <returns>A <see cref="StatusCodeResult"/> with the given <paramref name="statusCode"/>.</returns>
		protected StatusCodeResult StatusCode(HttpStatusCode statusCode) => StatusCode((int)statusCode);

		/// <summary>
		/// 429 response for a given <paramref name="rateLimitException"/>.
		/// </summary>
		/// <param name="rateLimitException">The <see cref="RateLimitExceededException"/> that occurred.</param>
		/// <returns>A <see cref="HttpStatusCode.TooManyRequests"/> <see cref="ObjectResult"/>.</returns>
		protected ObjectResult RateLimit(RateLimitExceededException rateLimitException)
		{
			ArgumentNullException.ThrowIfNull(rateLimitException);

			Logger.LogWarning(rateLimitException, "Exceeded GitHub rate limit!");

			var secondsString = Math.Ceiling(rateLimitException.GetRetryAfterTimeSpan().TotalSeconds).ToString(CultureInfo.InvariantCulture);
			Response.Headers.Add(HeaderNames.RetryAfter, secondsString);
			return this.StatusCode(HttpStatusCode.TooManyRequests, new ErrorMessageResponse(ErrorCode.GitHubApiRateLimit));
		}

		/// <summary>
		/// Performs validation a request.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in an appropriate <see cref="IActionResult"/> on validation failure, <see langword="null"/> otherwise.</returns>
		protected virtual ValueTask<IActionResult?> ValidateRequest(CancellationToken cancellationToken)
			=> ValueTask.FromResult<IActionResult?>(null);

		/// <summary>
		/// Response for missing/Invalid headers.
		/// </summary>
		/// <param name="headersException">The <see cref="HeadersException"/> that occurred while trying to parse the <see cref="ApiHeaders"/>.</param>
		/// <returns>The appropriate <see cref="IActionResult"/>.</returns>
		protected IActionResult HeadersIssue(HeadersException headersException)
		{
			if (headersException == null)
				throw new InvalidOperationException("Expected a header parse exception!");

			var errorMessage = new ErrorMessageResponse(ErrorCode.BadHeaders)
			{
				AdditionalData = headersException.Message,
			};

			if (headersException.ParseErrors.HasFlag(HeaderErrorTypes.Accept))
				return this.StatusCode(HttpStatusCode.NotAcceptable, errorMessage);

			return BadRequest(errorMessage);
		}

		/// <summary>
		/// Generates a paginated response.
		/// </summary>
		/// <typeparam name="TModel">The <see cref="Type"/> of model being generated and returned.</typeparam>
		/// <param name="queryGenerator">A <see cref="Func{TResult}"/> resulting in a <see cref="Task{TResult}"/> resulting in the generated <see cref="PaginatableResult{TModel}"/>.</param>
		/// <param name="resultTransformer">Optional <see cref="Func{T, TResult}"/> to transform the <typeparamref name="TModel"/>s after being queried.</param>
		/// <param name="pageQuery">The requested page from the query.</param>
		/// <param name="pageSizeQuery">The requested page size from the query.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		protected ValueTask<IActionResult> Paginated<TModel>(
			Func<ValueTask<PaginatableResult<TModel>>> queryGenerator,
			Func<TModel, ValueTask>? resultTransformer,
			int? pageQuery,
			int? pageSizeQuery,
			CancellationToken cancellationToken) => PaginatedImpl(
				queryGenerator,
				resultTransformer,
				pageQuery,
				pageSizeQuery,
				cancellationToken);

		/// <summary>
		/// Generates a paginated response.
		/// </summary>
		/// <typeparam name="TModel">The <see cref="Type"/> of model being generated.</typeparam>
		/// <typeparam name="TApiModel">The <see cref="Type"/> of model being returned.</typeparam>
		/// <param name="queryGenerator">A <see cref="Func{TResult}"/> resulting in a <see cref="ValueTask{TResult}"/> resulting in the generated <see cref="PaginatableResult{TModel}"/>.</param>
		/// <param name="resultTransformer">A <see cref="Func{T, TResult}"/> to transform the <typeparamref name="TApiModel"/>s after being queried.</param>
		/// <param name="pageQuery">The requested page from the query.</param>
		/// <param name="pageSizeQuery">The requested page size from the query.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		protected ValueTask<IActionResult> Paginated<TModel, TApiModel>(
			Func<ValueTask<PaginatableResult<TModel>>> queryGenerator,
			Func<TApiModel, ValueTask>? resultTransformer,
			int? pageQuery,
			int? pageSizeQuery,
			CancellationToken cancellationToken)
			where TModel : IApiTransformable<TApiModel>
			=> PaginatedImpl(
				queryGenerator,
				resultTransformer,
				pageQuery,
				pageSizeQuery,
				cancellationToken);

		/// <summary>
		/// Generates a paginated response.
		/// </summary>
		/// <typeparam name="TModel">The <see cref="Type"/> of model being generated. If different from <typeparamref name="TResultModel"/>, must implement <see cref="IApiTransformable{TApiModel}"/> for <typeparamref name="TResultModel"/>.</typeparam>
		/// <typeparam name="TResultModel">The <see cref="Type"/> of model being returned.</typeparam>
		/// <param name="queryGenerator">A <see cref="Func{TResult}"/> resulting in a <see cref="ValueTask{TResult}"/> resulting in the generated <see cref="PaginatableResult{TModel}"/>.</param>
		/// <param name="resultTransformer">A <see cref="Func{T, TResult}"/> to transform the <typeparamref name="TResultModel"/>s after being queried.</param>
		/// <param name="pageQuery">The requested page from the query.</param>
		/// <param name="pageSizeQuery">The requested page size from the query.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		async ValueTask<IActionResult> PaginatedImpl<TModel, TResultModel>(
			Func<ValueTask<PaginatableResult<TModel>>> queryGenerator,
			Func<TResultModel, ValueTask>? resultTransformer,
			int? pageQuery,
			int? pageSizeQuery,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(queryGenerator);

			if (pageQuery <= 0 || pageSizeQuery <= 0)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ApiInvalidPageOrPageSize));

			var pageSize = pageSizeQuery ?? DefaultPageSize;
			if (pageSize > MaximumPageSize)
				return BadRequest(new ErrorMessageResponse(ErrorCode.ApiPageTooLarge)
				{
					AdditionalData = $"Maximum page size: {MaximumPageSize}",
				});

			var page = pageQuery ?? 1;

			var paginationResult = await queryGenerator();
			if (!paginationResult.Valid)
				return paginationResult.EarlyOut;

			var queriedResults = paginationResult
				.Results
				.Skip((page - 1) * pageSize)
				.Take(pageSize);

			int totalResults;
			List<TModel> pagedResults;
			if (queriedResults.Provider is IAsyncQueryProvider)
			{
				totalResults = await paginationResult.Results.CountAsync(cancellationToken);
				pagedResults = await queriedResults
					.ToListAsync(cancellationToken);
			}
			else
			{
				totalResults = paginationResult.Results.Count();
				pagedResults = queriedResults.ToList();
			}

			ICollection<TResultModel> finalResults;
			if (typeof(TModel) == typeof(TResultModel))
				finalResults = (List<TResultModel>)(object)pagedResults; // clearly a safe cast
			else
				finalResults = pagedResults
					.OfType<IApiTransformable<TResultModel>>()
					.Select(x => x.ToApi())
					.ToList();

			if (resultTransformer != null)
				foreach (var finalResult in finalResults)
					await resultTransformer(finalResult);

			var carryTheOne = totalResults % pageSize != 0
				? 1
				: 0;
			return Json(
				new PaginatedResponse<TResultModel>
				{
					Content = finalResults,
					PageSize = pageSize,
					TotalPages = (ushort)(totalResults / pageSize) + carryTheOne,
					TotalItems = totalResults,
				});
		}
	}
}
