using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Octokit;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// A <see cref="Controller"/> for API functions
	/// </summary>
	[Produces(MediaTypeNames.Application.Json)]
	[ApiController]
	public abstract class ApiController : Controller
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
		/// The <see cref="ApiHeaders"/> for the operation
		/// </summary>
		protected ApiHeaders ApiHeaders { get; private set; }

		/// <summary>
		/// The <see cref="IDatabaseContext"/> for the operation
		/// </summary>
		protected IDatabaseContext DatabaseContext { get; }

		/// <summary>
		/// The <see cref="IAuthenticationContext"/> for the operation
		/// </summary>
		protected IAuthenticationContext AuthenticationContext { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="ApiController"/>
		/// </summary>
		protected ILogger<ApiController> Logger { get; }

		/// <summary>
		/// The <see cref="Instance"/> for the operation
		/// </summary>
		protected Models.Instance Instance { get; }

		/// <summary>
		/// If <see cref="ApiHeaders"/> are required
		/// </summary>
		readonly bool requireHeaders;

		/// <summary>
		/// Construct an <see cref="ApiController"/>
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="DatabaseContext"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="requireHeaders">The value of <see cref="requireHeaders"/></param>
		protected ApiController(
			IDatabaseContext databaseContext,
			IAuthenticationContextFactory authenticationContextFactory,
			ILogger<ApiController> logger,
			bool requireHeaders)
		{
			DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			if (authenticationContextFactory == null)
				throw new ArgumentNullException(nameof(authenticationContextFactory));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			AuthenticationContext = authenticationContextFactory.CurrentAuthenticationContext;
			Instance = AuthenticationContext?.InstancePermissionSet?.Instance;
			this.requireHeaders = requireHeaders;
		}

		/// <summary>
		/// Generic 410 response.
		/// </summary>
		/// <returns>An <see cref="ObjectResult"/> with <see cref="HttpStatusCode.Gone"/>.</returns>
		protected ObjectResult Gone() => StatusCode(HttpStatusCode.Gone, new ErrorMessage(ErrorCode.ResourceNotPresent));

		/// <summary>
		/// Generic 404 response.
		/// </summary>
		/// <returns>An <see cref="ObjectResult"/> with <see cref="HttpStatusCode.NotFound"/>.</returns>
		protected new NotFoundObjectResult NotFound() => NotFound(new ErrorMessage(ErrorCode.ResourceNeverPresent));

		/// <summary>
		/// Generic 501 response.
		/// </summary>
		/// <returns>An <see cref="ObjectResult"/> with <see cref="HttpStatusCode.NotImplemented"/>.</returns>
		protected ObjectResult RequiresPosixSystemIdentity() => StatusCode(HttpStatusCode.NotImplemented, new ErrorMessage(ErrorCode.RequiresPosixSystemIdentity));

		/// <summary>
		/// Strongly type calls to <see cref="ControllerBase.StatusCode(int)"/>.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
		/// <returns>A <see cref="StatusCodeResult"/> with the given <paramref name="statusCode"/>.</returns>
		protected StatusCodeResult StatusCode(HttpStatusCode statusCode) => StatusCode((int)statusCode);

		/// <summary>
		/// Strongly type calls to <see cref="ControllerBase.StatusCode(int, object)"/>.
		/// </summary>
		/// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
		/// <param name="errorMessage">The accompanying <see cref="ErrorMessage"/> payload.</param>
		/// <returns>A <see cref="StatusCodeResult"/> with the given <paramref name="statusCode"/>.</returns>
		protected ObjectResult StatusCode(HttpStatusCode statusCode, object errorMessage) => StatusCode((int)statusCode, errorMessage);

		/// <summary>
		/// Generic 201 response with a given <paramref name="payload"/>.
		/// </summary>
		/// <param name="payload">The accompanying API payload.</param>
		/// <returns>A <see cref="HttpStatusCode.Created"/> <see cref="ObjectResult"/> with the given <paramref name="payload"/>.</returns>
		protected ObjectResult Created(object payload) => StatusCode((int)HttpStatusCode.Created, payload);

		/// <summary>
		/// 429 response for a given <paramref name="rateLimitException"/>.
		/// </summary>
		/// <param name="rateLimitException">The <see cref="RateLimitExceededException"/> that occurred.</param>
		/// <returns>A <see cref="HttpStatusCode.TooManyRequests"/> <see cref="ObjectResult"/>.</returns>
		protected ObjectResult RateLimit(RateLimitExceededException rateLimitException)
		{
			if (rateLimitException == null)
				throw new ArgumentNullException(nameof(rateLimitException));

			Logger.LogWarning(rateLimitException, "Exceeded GitHub rate limit!");
			var secondsString = Math.Ceiling((rateLimitException.Reset - DateTimeOffset.Now).TotalSeconds).ToString(CultureInfo.InvariantCulture);
			Response.Headers.Add(HeaderNames.RetryAfter, secondsString);
			return StatusCode(HttpStatusCode.TooManyRequests, new ErrorMessage(ErrorCode.GitHubApiRateLimit));
		}

		/// <summary>
		/// Performs validation a request.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in an appropriate <see cref="IActionResult"/> on validation failure, <see langword="null"/> otherwise.</returns>
		protected virtual Task<IActionResult> ValidateRequest(CancellationToken cancellationToken)
			=> Task.FromResult<IActionResult>(null);

		/// <summary>
		/// Response for missing/Invalid headers.
		/// </summary>
		/// <param name="ignoreMissingAuth">Whether or not errors due to missing <see cref="HeaderNames.Authorization"/> should be thrown.</param>
		/// <returns>The appropriate <see cref="IActionResult"/>.</returns>
		protected IActionResult HeadersIssue(bool ignoreMissingAuth)
		{
			HeadersException headersException;
			try
			{
				var _ = new ApiHeaders(Request.GetTypedHeaders(), ignoreMissingAuth);
				throw new InvalidOperationException("Expected a header parse exception!");
			}
			catch (HeadersException ex)
			{
				headersException = ex;
			}

			var errorMessage = new ErrorMessage(ErrorCode.BadHeaders)
			{
				AdditionalData = headersException.Message
			};

			if (headersException.MissingOrMalformedHeaders.HasFlag(HeaderTypes.Accept))
				return StatusCode(HttpStatusCode.NotAcceptable, errorMessage);

			if (headersException.MissingOrMalformedHeaders == HeaderTypes.Authorization)
				return Unauthorized(errorMessage);

			return BadRequest(errorMessage);
		}

		/// <inheritdoc />
#pragma warning disable CA1506 // TODO: Decomplexify
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			if (context == null)
				throw new ArgumentNullException(nameof(context));

			// ALL valid token and login requests that match a route go through this function
			// 404 is returned before
			if (AuthenticationContext != null && AuthenticationContext.User == null)
			{
				// valid token, expired password
				await Unauthorized().ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			// validate the headers
			try
			{
				ApiHeaders = new ApiHeaders(Request.GetTypedHeaders());

				if (!ApiHeaders.Compatible())
				{
					await StatusCode(
						HttpStatusCode.UpgradeRequired,
						new ErrorMessage(ErrorCode.ApiMismatch))
						.ExecuteResultAsync(context)
						.ConfigureAwait(false);
					return;
				}

				var errorCase = await ValidateRequest(context.HttpContext.RequestAborted).ConfigureAwait(false);
				if (errorCase != null)
				{
					await errorCase.ExecuteResultAsync(context).ConfigureAwait(false);
					return;
				}
			}
			catch (HeadersException)
			{
				if (requireHeaders)
				{
					await HeadersIssue(false)
						.ExecuteResultAsync(context)
						.ConfigureAwait(false);
					return;
				}
			}

			if (ModelState?.IsValid == false)
			{
				var errorMessages = ModelState
					.SelectMany(x => x.Value.Errors)
					.Select(x => x.ErrorMessage)

					// We use RequiredAttributes purely for preventing properties from becoming nullable in the databases
					// We validate missing required fields in controllers
					// Unfortunately, we can't remove the whole validator for that as it checks other things like StringLength
					// This is the best way to deal with it unfortunately
					.Where(x => !x.EndsWith(" field is required.", StringComparison.Ordinal));

				if (errorMessages.Any())
				{
					await BadRequest(
						new ErrorMessage(ErrorCode.ModelValidationFailure)
						{
							AdditionalData = String.Join(Environment.NewLine, errorMessages)
						})
						.ExecuteResultAsync(context).ConfigureAwait(false);
					return;
				}

				ModelState.Clear();
			}

			using (ApiHeaders?.InstanceId != null
				? LogContext.PushProperty("Instance", ApiHeaders.InstanceId)
				: null)
			using (AuthenticationContext != null
				? LogContext.PushProperty("User", AuthenticationContext.User.Id)
				: null)
			using (LogContext.PushProperty("Request", $"{Request.Method} {Request.Path}"))
			{
				if (ApiHeaders != null)
					Logger.LogDebug(
						"Starting API request: Version: {0}. {1}: {2}",
						ApiHeaders.ApiVersion.Semver(),
						HeaderNames.UserAgent,
						ApiHeaders.RawUserAgent);
				else if (Request.Headers.TryGetValue(HeaderNames.UserAgent, out var userAgents))
					Logger.LogDebug(
						"Starting unauthorized API request. {0}: {1}",
						HeaderNames.UserAgent,
						userAgents);
				else
					Logger.LogDebug(
						"Starting unauthorized API request. No {0}!",
						HeaderNames.UserAgent);
				await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
			}
		}
#pragma warning restore CA1506

		/// <summary>
		/// Generates a paginated response.
		/// </summary>
		/// <typeparam name="TModel">The <see cref="Type"/> of model being generated and returned.</typeparam>
		/// <param name="queryGenerator">A <see cref="Func{TResult}"/> resulting in a <see cref="Task{TResult}"/> resulting in the generated <see cref="PaginatableResult{TModel}"/>.</param>
		/// <param name="resultTransformer">An <see cref="Action{T1}"/> to transform the <typeparamref name="TModel"/>s after being queried.</param>
		/// <param name="pageQuery">The requested page from the query.</param>
		/// <param name="pageSizeQuery">The requested page size from the query.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		protected Task<IActionResult> Paginated<TModel>(
			Func<Task<PaginatableResult<TModel>>> queryGenerator,
			Action<TModel> resultTransformer,
			int? pageQuery,
			int? pageSizeQuery,
			CancellationToken cancellationToken) => PaginatedImpl<TModel, TModel>(
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
		/// <param name="queryGenerator">A <see cref="Func{TResult}"/> resulting in a <see cref="Task{TResult}"/> resulting in the generated <see cref="PaginatableResult{TModel}"/>.</param>
		/// <param name="resultTransformer">An <see cref="Action{T1}"/> to transform the <typeparamref name="TModel"/>s after being queried.</param>
		/// <param name="pageQuery">The requested page from the query.</param>
		/// <param name="pageSizeQuery">The requested page size from the query.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		protected Task<IActionResult> Paginated<TModel, TApiModel>(
			Func<Task<PaginatableResult<TModel>>> queryGenerator,
			Action<TModel> resultTransformer,
			int? pageQuery,
			int? pageSizeQuery,
			CancellationToken cancellationToken)
			where TModel : IApiTransformable<TApiModel>
			=> PaginatedImpl<TModel, TApiModel>(
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
		/// <param name="queryGenerator">A <see cref="Func{TResult}"/> resulting in a <see cref="Task{TResult}"/> resulting in the generated <see cref="PaginatableResult{TModel}"/>.</param>
		/// <param name="resultTransformer">An <see cref="Action{T1}"/> to transform the <typeparamref name="TModel"/>s after being queried.</param>
		/// <param name="pageQuery">The requested page from the query.</param>
		/// <param name="pageSizeQuery">The requested page size from the query.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="IActionResult"/> for the operation.</returns>
		async Task<IActionResult> PaginatedImpl<TModel, TResultModel>(
			Func<Task<PaginatableResult<TModel>>> queryGenerator,
			Action<TModel> resultTransformer,
			int? pageQuery,
			int? pageSizeQuery,
			CancellationToken cancellationToken)
		{
			if (queryGenerator == null)
				throw new ArgumentNullException(nameof(queryGenerator));

			if (pageQuery <= 0 || pageSizeQuery <= 0)
				return BadRequest(new ErrorMessage(ErrorCode.ApiInvalidPageOrPageSize));

			var pageSize = pageSizeQuery ?? DefaultPageSize;
			if (pageSize > MaximumPageSize)
				return BadRequest(new ErrorMessage(ErrorCode.ApiPageTooLarge)
				{
					AdditionalData = $"Maximum page size: {MaximumPageSize}"
				});

			var page = pageQuery ?? 1;

			var paginationResult = await queryGenerator().ConfigureAwait(false);
			if (paginationResult.EarlyOut != null)
				return paginationResult.EarlyOut;

			var queriedResults = paginationResult
				.Results
				.Skip((page - 1) * pageSize)
				.Take(pageSize);

			int totalResults;
			List<TModel> pagedResults;
			if (queriedResults.Provider is IAsyncQueryProvider)
			{
				totalResults = await paginationResult.Results.CountAsync(cancellationToken).ConfigureAwait(false);
				pagedResults = await queriedResults
					.ToListAsync(cancellationToken)
					.ConfigureAwait(false);
			}
			else
			{
				totalResults = paginationResult.Results.Count();
				pagedResults = queriedResults.ToList();
			}

			if (resultTransformer != null)
				foreach (var I in pagedResults)
					resultTransformer(I);

			ICollection<TResultModel> finalResults;
			if (typeof(TModel) == typeof(TResultModel))
				finalResults = (List<TResultModel>)(object)pagedResults; // clearly a safe cast
			else
				finalResults = pagedResults
					.OfType<IApiTransformable<TResultModel>>()
					.Select(x => x.ToApi())
					.ToList();

			return Json(
				new Paginated<TModel>
				{
					Content = pagedResults,
					PageSize = pageSize,
					TotalPages = (ushort)((totalResults % pageSize) + 1)
				});
		}
	}
}
