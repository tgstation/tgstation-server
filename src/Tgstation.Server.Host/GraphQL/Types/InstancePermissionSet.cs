using System;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.GraphQL.Types
{
	/// <summary>
	/// Represents a set of permissions for an <see cref="Instance"/>.
	/// </summary>
	public sealed class InstancePermissionSet
	{
		/// <summary>
		/// Gets the <see cref="Types.PermissionSet"/> the <see cref="InstancePermissionSet"/> belongs to.
		/// </summary>
		/// <returns>The owning <see cref="Types.PermissionSet"/>.</returns>
		public PermissionSet PermissionSet()
			=> throw new NotImplementedException();

		/// <summary>
		/// The <see cref="Api.Rights.InstancePermissionSetRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public RightsHolder<InstancePermissionSetRights> InstancePermissionSetRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.EngineRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public RightsHolder<EngineRights> EngineRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.DreamDaemonRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public RightsHolder<DreamDaemonRights> DreamDaemonRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.DreamMakerRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public RightsHolder<DreamMakerRights> DreamMakerRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.RepositoryRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public RightsHolder<RepositoryRights> RepositoryRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.ChatBotRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public RightsHolder<ChatBotRights> ChatBotRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.ConfigurationRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public RightsHolder<ConfigurationRights> ConfigurationRights { get; set; }
	}
}
