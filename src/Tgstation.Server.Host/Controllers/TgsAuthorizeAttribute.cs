using System;

using Microsoft.AspNetCore.Authorization;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Helper for using the <see cref="AuthorizeAttribute"/> with the <see cref="Api.Rights"/> system.
	/// </summary>
#pragma warning disable CA1019
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	sealed class TgsAuthorizeAttribute : AuthorizeAttribute
	{
		/// <summary>
		/// Gets the <see cref="Api.Rights.RightsType"/> associated with the <see cref="TgsAuthorizeAttribute"/> if any.
		/// </summary>
		public RightsType? RightsType { get; }

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
		/// <param name="requiredRights">The <see cref="ByondRights"/> required.</param>
		public TgsAuthorizeAttribute(ByondRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Byond;
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
	}
}
