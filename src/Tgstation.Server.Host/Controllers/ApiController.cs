using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Rights;
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
		/// Runs after a <see cref="Api.Models.Token"/> has been validated. Creates the <see cref="IAuthenticationContext"/> for the <see cref="ControllerBase.Request"/>
		/// </summary>
		/// <param name="context">The <see cref="TokenValidatedContext"/> for the operation</param>
		/// <returns>A <see cref="Task"/> representing the running operation</returns>
		public static async Task OnTokenValidated(TokenValidatedContext context)
		{
			var databaseContext = context.HttpContext.RequestServices.GetRequiredService<IDatabaseContext>();
			var authenticationContextFactory = context.HttpContext.RequestServices.GetRequiredService<IAuthenticationContextFactory>();

			var userIdClaim = context.Principal.FindFirst(JwtRegisteredClaimNames.Sub);

			if (userIdClaim == default(Claim))
				throw new InvalidOperationException("Missing required claim!");

			long userId;
			try
			{
				userId = Int64.Parse(userIdClaim.Value, CultureInfo.InvariantCulture);
			}
			catch (Exception e)
			{
				throw new InvalidOperationException("Failed to parse user ID!", e);
			}
			
			ApiHeaders apiHeaders;
			try
			{
				apiHeaders = new ApiHeaders(context.HttpContext.Request.GetTypedHeaders());
			}
			catch
			{
				//let OnActionExecutionAsync handle the reponse
				return;
			}

			await authenticationContextFactory.CreateAuthenticationContext(userId, apiHeaders.InstanceId, context.SecurityToken.ValidFrom, context.HttpContext.RequestAborted).ConfigureAwait(false);

			var authenticationContext = authenticationContextFactory.CurrentAuthenticationContext;

			var enumerator = Enum.GetValues(typeof(RightsType));
			var claims = new List<Claim>();
			foreach (RightsType I in enumerator)
			{
				//if there's no instance user, do a weird thing and add all the instance roles
				//we need it so we can get to OnActionExecutionAsync where we can properly decide between BadRequest and Forbid
				//if user is null that means they got the token with an expired password
				var rightInt = authenticationContext.User == null || (RightsHelper.IsInstanceRight(I) && authenticationContext.InstanceUser == null) ? ~0 : authenticationContext.GetRight(I);
				var rightEnum = RightsHelper.RightToType(I);
				var right = (Enum)Enum.ToObject(rightEnum, rightInt);
				foreach (Enum J in Enum.GetValues(rightEnum))
					if (right.HasFlag(J))
						claims.Add(new Claim(ClaimTypes.Role, RightsHelper.RoleName(I, J)));
			}

			context.Principal.AddIdentity(new ClaimsIdentity(claims));
		}

		/// <summary>
		/// Construct an <see cref="ApiController"/>
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="DatabaseContext"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		/// <param name="logger">The value of <see cref="Logger"/></param>
		/// <param name="requireInstance">The value of <see cref="requireInstance"/></param>
		public ApiController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory, ILogger logger, bool requireInstance)
		{
			DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			if (authenticationContextFactory == null)
				throw new ArgumentNullException(nameof(authenticationContextFactory));
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			AuthenticationContext = authenticationContextFactory.CurrentAuthenticationContext;
			Instance = AuthenticationContext?.InstanceUser?.Instance;
			this.requireInstance = requireInstance;
		}
		
		/// <inheritdoc />
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			if (AuthenticationContext != null && AuthenticationContext.User == null)
			{
				//valid token, expired password
				await Unauthorized().ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			//validate the headers
			try
			{
				ApiHeaders = new ApiHeaders(Request.GetTypedHeaders());

				if (requireInstance)
				{
					if(!ApiHeaders.InstanceId.HasValue)
					{
						await BadRequest(new ErrorMessage { Message = "Missing Instance header!" }).ExecuteResultAsync(context).ConfigureAwait(false);
						return;
					}
					if (AuthenticationContext.InstanceUser == null)
					{
						//accessing an instance they don't have access to or one that's disabled
						await Forbid().ExecuteResultAsync(context).ConfigureAwait(false);
						return;
					}
				}
			}
			catch (InvalidOperationException e)
			{
				await BadRequest(new ErrorMessage { Message = e.Message }).ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			if(ModelState?.IsValid == false)
			{
				var errorMessages = ModelState.SelectMany(x => x.Value.Errors).Select(x => x.ErrorMessage).ToList();
				//do some fuckery to remove RequiredAttribute errors
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

			Logger.LogTrace("Request made by User ID {0}. Api version: {1}. User-Agent: {2}. Type: {3}. Route {4}{5}", AuthenticationContext?.User.Id.ToString(CultureInfo.InvariantCulture), ApiHeaders.ApiVersion, ApiHeaders.UserAgent, Request.Method, Request.Path, Request.QueryString);

			await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
		}
	}
}
