using System;

using HotChocolate.Authorization;

using Microsoft.AspNetCore.Mvc.Filters;

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
		public const string CoreAccessRole = "GRAPH_QL_CORE_ACCESS";

		/// <summary>
		/// Gets the <see cref="Api.Rights.RightsType"/> associated with the <see cref="TgsAuthorizeAttribute"/> if any.
		/// </summary>
		public RightsType? RightsType { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TgsGraphQLAuthorizeAttribute"/> class.
		/// </summary>
		public TgsGraphQLAuthorizeAttribute()
		{
			Roles = [CoreAccessRole];
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

		private TgsGraphQLAuthorizeAttribute(string roleNames)
		{
			Roles = $"{CoreAccessRole},{roleNames}".Split(',');
		}
	}
}
