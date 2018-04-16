using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;

namespace Tgstation.Server.Host.Controllers
{
	[Produces(ApiHeaders.ApplicationJson)]
	[Consumes(ApiHeaders.ApplicationJson)]
	public abstract class ApiController : Controller
	{
		protected ApiHeaders ApiHeaders { get; private set; }

		protected IDatabaseContext DatabaseContext { get; }

		protected IAuthenticationContext AuthenticationContext { get; }

		protected Instance Instance { get; }

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
			var claims = new List<Claim>
			{
				Capacity = enumerator.Length
			};
			foreach (RightsType I in enumerator)
				claims.Add(new Claim(ClaimTypes.Role, RightsHelper.RoleName(I, authenticationContext.GetRight(I))));

			context.Principal.AddIdentity(new ClaimsIdentity(claims));
		}

		public ApiController(IDatabaseContext databaseContext, IAuthenticationContextFactory authenticationContextFactory)
		{
			DatabaseContext = databaseContext ?? throw new ArgumentNullException(nameof(databaseContext));
			if (authenticationContextFactory == null)
				throw new ArgumentNullException(nameof(authenticationContextFactory));
			AuthenticationContext = authenticationContextFactory.CurrentAuthenticationContext;
			Instance = AuthenticationContext?.InstanceUser?.Instance;
		}
		
		public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
		{
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
