using System;
using Microsoft.AspNetCore.Authorization;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Helper for using the <see cref="AuthorizeAttribute"/> with the <see cref="Api.Rights"/> system
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
	sealed class TgsAuthorizeAttribute : AuthorizeAttribute
	{
		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/>
		/// </summary>
		public TgsAuthorizeAttribute() { }

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="AdministrationRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(AdministrationRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="InstanceManagerRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(InstanceManagerRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="RepositoryRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(RepositoryRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="ByondRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(ByondRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="DreamMakerRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(DreamMakerRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="DreamDaemonRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(DreamDaemonRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="ChatSettingsRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(ChatSettingsRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="ConfigurationRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(ConfigurationRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);

		/// <summary>
		/// Construct a <see cref="TgsAuthorizeAttribute"/> for <see cref="InstanceUserRights"/>
		/// </summary>
		/// <param name="requiredRights">The rights required</param>
		public TgsAuthorizeAttribute(InstanceUserRights requiredRights) => Roles = RightsHelper.RoleNames(requiredRights);
	}
}
