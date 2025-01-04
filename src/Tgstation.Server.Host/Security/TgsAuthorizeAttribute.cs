using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Authorization;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Helper for using the <see cref="AuthorizeAttribute"/> with the <see cref="Api.Rights"/> system.
	/// </summary>
#pragma warning disable CA1019
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	sealed class TgsAuthorizeAttribute : AuthorizeAttribute
	{
		/// <summary>
		/// Policy used to apply global requirement of <see cref="UserEnabledRole"/>.
		/// </summary>
		public const string PolicyName = "Policy.UserEnabled";

		/// <summary>
		/// Role used to indicate access to the server is allowed.
		/// </summary>
		public const string UserEnabledRole = "Role.UserEnabled";

		/// <summary>
		/// Gets the <see cref="Api.Rights.RightsType"/> associated with the <see cref="TgsAuthorizeAttribute"/> if any.
		/// </summary>
		public RightsType? RightsType { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		public TgsAuthorizeAttribute()
			: this(Enumerable.Empty<string>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="AdministrationRights"/> required.</param>
		public TgsAuthorizeAttribute(AdministrationRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.Administration;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="InstanceManagerRights"/> required.</param>
		public TgsAuthorizeAttribute(InstanceManagerRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.InstanceManager;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="RepositoryRights"/> required.</param>
		public TgsAuthorizeAttribute(RepositoryRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.Repository;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="EngineRights"/> required.</param>
		public TgsAuthorizeAttribute(EngineRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.Engine;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="DreamMakerRights"/> required.</param>
		public TgsAuthorizeAttribute(DreamMakerRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.DreamMaker;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="DreamDaemonRights"/> required.</param>
		public TgsAuthorizeAttribute(DreamDaemonRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.DreamDaemon;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="ChatBotRights"/> required.</param>
		public TgsAuthorizeAttribute(ChatBotRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.ChatBots;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="ConfigurationRights"/> required.</param>
		public TgsAuthorizeAttribute(ConfigurationRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.Configuration;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="InstancePermissionSetRights"/> required.</param>
		public TgsAuthorizeAttribute(InstancePermissionSetRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.InstancePermissionSet;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="roles">An <see cref="IEnumerable{T}"/> of roles to be required alongside the <see cref="UserEnabledRole"/>.</param>
		private TgsAuthorizeAttribute(IEnumerable<string> roles)
		{
			var listRoles = roles.ToList();
			if (listRoles.Count != 0)
			{
				Roles = String.Join(",", listRoles);
			}

			Policy = PolicyName;
		}
	}
}
