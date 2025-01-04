using System;
using System.Collections.Generic;
using System.Linq;

using HotChocolate.Authorization;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// Helper for using the <see cref="AuthorizeAttribute"/> with the <see cref="Api.Rights"/> system.
	/// </summary>
#pragma warning disable CA1019
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
	sealed class TgsGraphQLAuthorizeAttribute : AuthorizeAttribute
	{
		/// <summary>
		/// Gets the <see cref="Api.Rights.RightsType"/> associated with the <see cref="TgsAuthorizeAttribute"/> if any.
		/// </summary>
		public RightsType? RightsType { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		public TgsGraphQLAuthorizeAttribute()
			: this(Enumerable.Empty<string>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="AdministrationRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(AdministrationRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.Administration;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="InstanceManagerRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(InstanceManagerRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.InstanceManager;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="RepositoryRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(RepositoryRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.Repository;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="EngineRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(EngineRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.Engine;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="DreamMakerRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(DreamMakerRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.DreamMaker;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="DreamDaemonRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(DreamDaemonRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.DreamDaemon;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="ChatBotRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(ChatBotRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.ChatBots;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="ConfigurationRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(ConfigurationRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.Configuration;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="requiredRights">The <see cref="InstancePermissionSetRights"/> required.</param>
		public TgsGraphQLAuthorizeAttribute(InstancePermissionSetRights requiredRights)
			: this(RightsHelper.RoleNames(requiredRights))
		{
			RightsType = Api.Rights.RightsType.InstancePermissionSet;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		/// <param name="roleNames"><see cref="IEnumerable{T}"/> of role names.</param>
		private TgsGraphQLAuthorizeAttribute(IEnumerable<string> roleNames)
		{
			var listRoles = roleNames.ToList();
			if (listRoles.Count != 0)
			{
				Roles = [.. listRoles];
			}

			Policy = TgsAuthorizeAttribute.PolicyName;
			Apply = ApplyPolicy.Validation;
		}
	}
}
