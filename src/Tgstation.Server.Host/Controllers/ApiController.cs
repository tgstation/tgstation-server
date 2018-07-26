using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Tgstation.Server.Api;
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
		/// The <see cref="Instance"/> for the operation
		/// </summary>
		protected Instance Instance { get; }

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

			await authenticationContextFactory.CreateAuthenticationContext(userId, apiHeaders.InstanceId, context.HttpContext.RequestAborted).ConfigureAwait(false);

			var authenticationContext = authenticationContextFactory.CurrentAuthenticationContext;

			var enumerator = Enum.GetValues(typeof(RightsType));
			var claims = new List<Claim>();
			foreach (RightsType I in enumerator)
			{
				var rightInt = authenticationContext.GetRight(I);
				var rightEnum = RightsHelper.RightToType(I);
				var right = (Enum)Enum.ToObject(rightEnum, authenticationContext.GetRight(I));
				foreach(Enum J in Enum.GetValues(rightEnum))
					if(right.HasFlag(J))
						claims.Add(new Claim(ClaimTypes.Role, RightsHelper.RoleName(I, J)));
			}

			context.Principal.AddIdentity(new ClaimsIdentity(claims));
		}

		/// <summary>
		/// Construct an <see cref="ApiController"/>
		/// </summary>
		/// <param name="databaseContext">The value of <see cref="DatabaseContext"/></param>
		/// <param name="authenticationContextFactory">The <see cref="IAuthenticationContextFactory"/> for the <see cref="ApiController"/></param>
		public ApiController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory)
		{
			DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			if (authenticationContextFactory == null)
				throw new ArgumentNullException(nameof(authenticationContextFactory));
			AuthenticationContext = authenticationContextFactory.CurrentAuthenticationContext;
			Instance = AuthenticationContext?.InstanceUser?.Instance;
		}
		
		/// <inheritdoc />
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
			if (AuthenticationContext == null)
			{
				//accessing an instance they don't have access to
				await Forbid().ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			//validate the headers
			try
			{
				ApiHeaders = new ApiHeaders(Request.GetTypedHeaders());
			}
			catch (InvalidOperationException e)
			{
				await BadRequest(new { message = e.Message }).ExecuteResultAsync(context).ConfigureAwait(false);
				return;
			}

			await base.OnActionExecutionAsync(context, next).ConfigureAwait(false);
		}
	}
}
