using System;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Helper for using the <see cref="AuthorizeAttribute"/> with the <see cref="Api.Rights"/> system.
	/// </summary>
#pragma warning disable CA1019
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	sealed class TgsAuthorizeAttribute : AuthorizeAttribute, IAuthorizationFilter
	{
		/// <summary>
		/// Gets the <see cref="Api.Rights.RightsType"/> associated with the <see cref="TgsAuthorizeAttribute"/> if any.
		/// </summary>
		public RightsType? RightsType { get; }

		/// <summary>
		/// Implementation of <see cref="IAuthorizationFilter.OnAuthorization(AuthorizationFilterContext)"/>.
		/// </summary>
		/// <param name="context">The <see cref="AuthorizationFilterContext"/>.</param>
		public static void OnAuthorizationHelper(AuthorizationFilterContext context)
		{
			ArgumentNullException.ThrowIfNull(context);

			var services = context.HttpContext.RequestServices;
			var authenticationContext = services.GetRequiredService<IAuthenticationContext>();
			var logger = services.GetRequiredService<ILogger<TgsAuthorizeAttribute>>();

			if (!authenticationContext.Valid)
			{
				logger.LogTrace("authenticationContext is invalid!");
				context.Result = new UnauthorizedResult();
				return;
			}

			if (authenticationContext.User.Require(x => x.Enabled))
				return;

			logger.LogTrace("authenticationContext is for a disabled user!");
			context.Result = new ForbidResult();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		public TgsAuthorizeAttribute()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="AdministrationRights"/> required.</param>
		public TgsAuthorizeAttribute(AdministrationRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Administration;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="InstanceManagerRights"/> required.</param>
		public TgsAuthorizeAttribute(InstanceManagerRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.InstanceManager;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="RepositoryRights"/> required.</param>
		public TgsAuthorizeAttribute(RepositoryRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Repository;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="EngineRights"/> required.</param>
		public TgsAuthorizeAttribute(EngineRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Engine;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="DreamMakerRights"/> required.</param>
		public TgsAuthorizeAttribute(DreamMakerRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.DreamMaker;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="DreamDaemonRights"/> required.</param>
		public TgsAuthorizeAttribute(DreamDaemonRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.DreamDaemon;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="ChatBotRights"/> required.</param>
		public TgsAuthorizeAttribute(ChatBotRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.ChatBots;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="ConfigurationRights"/> required.</param>
		public TgsAuthorizeAttribute(ConfigurationRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Configuration;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="InstancePermissionSetRights"/> required.</param>
		public TgsAuthorizeAttribute(InstancePermissionSetRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.InstancePermissionSet;
		}

		/// <inheritdoc />
		public void OnAuthorization(AuthorizationFilterContext context)
			=> OnAuthorizationHelper(context);
	}
}
