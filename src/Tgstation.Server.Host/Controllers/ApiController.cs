using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Database;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// A <see cref="Controller"/> for API functions
	/// </summary>
	[Produces(ApiHeaders.ApplicationJson)]
	[ApiController]
	public abstract class ApiController : Controller
	{
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
		protected ILogger Logger { get; }

		/// <summary>
		/// The <see cref="Instance"/> for the operation
		/// </summary>
		protected Models.Instance Instance { get; }

		/// <summary>
		/// If <see cref="IAuthenticationContext.InstanceUser"/> permissions are required to access the <see cref="ApiController"/>
		/// </summary>
		readonly bool requireInstance;

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
		/// <param name="requireInstance">The value of <see cref="requireInstance"/></param>
		/// <param name="requireHeaders">The value of <see cref="requireHeaders"/></param>
		public ApiController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, ILogger logger, bool requireInstance, bool requireHeaders)
		{
			DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			if (authenticationContextFactory == null)
				throw new ArgumentNullException(nameof(authenticationContextFactory));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			AuthenticationContext = authenticationContextFactory.CurrentAuthenticationContext;
			Instance = AuthenticationContext?.InstanceUser?.Instance;
			this.requireInstance = requireInstance;
			this.requireHeaders = requireHeaders;
		}

		/// <inheritdoc />
		#pragma warning disable CA1506 // TODO: Decomplexify
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
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
						(int)HttpStatusCode.UpgradeRequired,
						new ErrorMessage(ErrorCode.ApiMismatch))
						.ExecuteResultAsync(context)
						.ConfigureAwait(false);
					return;
				}

				if (requireInstance)
				{
					if (!ApiHeaders.InstanceId.HasValue)
					{
						await BadRequest(new ErrorMessage(ErrorCode.InstanceHeaderRequired)).ExecuteResultAsync(context).ConfigureAwait(false);
						return;
					}

					if (AuthenticationContext.InstanceUser == null)
					{
						// accessing an instance they don't have access to or one that's disabled
						await Forbid().ExecuteResultAsync(context).ConfigureAwait(false);
						return;
					}
				}
			}
			catch (InvalidOperationException e)
			{
				if (requireHeaders)
				{
					await BadRequest(
						new ErrorMessage(ErrorCode.BadHeaders)
						{
							AdditionalData = e.Message
						})
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
						"Starting API Request: Version: {1}. User-Agent: {2}",
						AuthenticationContext?.User.Id.Value.ToString(CultureInfo.InvariantCulture),
						ApiHeaders.ApiVersion.Semver(),
						ApiHeaders.RawUserAgent,
						Request.Method,
						Request.Path,
						Request.QueryString,
						ApiHeaders.InstanceId);
				await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
			}
		}
		#pragma warning restore CA1506
	}
}
