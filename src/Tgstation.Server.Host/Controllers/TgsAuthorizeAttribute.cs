using System;
using Microsoft.AspNetCore.Authorization;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Helper for using the <see cref="AuthorizeAttribute"/> with the <see cref="Api.Rights"/> system
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
		/// Construct a <see cref="TgsAuthorizeAttribute"/>
		/// </summary>
		public TgsAuthorizeAttribute() { }

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="AdministrationRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(AdministrationRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Administration;
		}

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="InstanceManagerRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(InstanceManagerRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.InstanceManager;
		}

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="RepositoryRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(RepositoryRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Repository;
		}

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="ByondRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(ByondRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Byond;
		}

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="DreamMakerRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(DreamMakerRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.DreamMaker;
		}

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="DreamDaemonRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(DreamDaemonRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.DreamDaemon;
		}

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="ChatBotRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(ChatBotRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.ChatBots;
		}

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="ConfigurationRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(ConfigurationRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.Configuration;
		}

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="InstancePermissionSetRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(InstancePermissionSetRights requiredRights)
		{
			Roles = RightsHelper.RoleNames(requiredRights);
			RightsType = Api.Rights.RightsType.InstancePermissionSet;
		}
	}
}
