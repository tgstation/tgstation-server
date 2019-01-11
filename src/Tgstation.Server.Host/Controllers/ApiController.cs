using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// A <see cref="Controller"/> for API functions
	/// </summary>
	[Produces(ApiHeaders.ApplicationJson)]
	[Consumes(ApiHeaders.ApplicationJson)]
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
					await StatusCode((int)HttpStatusCode.UpgradeRequired, new ErrorMessage
					{
						Message = "Provided API version is incompatible with server version!"
					}).ExecuteResultAsync(context).ConfigureAwait(false);
					return;
				}

				if (requireInstance)
				{
					if (!ApiHeaders.InstanceId.HasValue)
					{
						await BadRequest(new ErrorMessage { Message = "Missing Instance header!" }).ExecuteResultAsync(context).ConfigureAwait(false);
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
					await BadRequest(new ErrorMessage { Message = e.Message }).ExecuteResultAsync(context).ConfigureAwait(false);
					return;
				}
			}

			if (ModelState?.IsValid == false)
			{
				var errorMessages = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage).ToList();

				// HACK
				// do some fuckery to remove RequiredAttribute errors
				for (var I = 0; I < errorMessages.Count; ++I)
				{
					var message = errorMessages[I];
					if (message.StartsWith("The ", StringComparison.Ordinal) && message.EndsWith(" field is required.", StringComparison.Ordinal))
					{
						errorMessages.RemoveAt(I);
						--I;
					}
				}

				if (errorMessages.Count > 0)
				{
					await BadRequest(new ErrorMessage { Message = String.Join(Environment.NewLine, errorMessages) }).ExecuteResultAsync(context).ConfigureAwait(false);
					return;
				}
			}

			if (ApiHeaders != null)
				Logger.LogDebug("Request made by User ID {0}. Api version: {1}. User-Agent: {2}. Type: {3}. Route {4}{5} to Instance {6}", AuthenticationContext?.User.Id.ToString(CultureInfo.InvariantCulture), ApiHeaders.ApiVersion, ApiHeaders.RawUserAgent, Request.Method, Request.Path, Request.QueryString, ApiHeaders.InstanceId);

			try
			{
				await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
			}
			catch (OperationCanceledException e)
			{
				Logger.LogDebug("Request cancelled! Exception: {0}", e);
				throw;
			}
		}
		#pragma warning restore CA1506
	}
}
