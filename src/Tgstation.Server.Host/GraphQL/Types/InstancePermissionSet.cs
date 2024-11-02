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
		public InstancePermissionSetRights? InstancePermissionSetRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.EngineRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public EngineRights? EngineRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.DreamDaemonRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public DreamDaemonRights? DreamDaemonRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.DreamMakerRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public DreamMakerRights? DreamMakerRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.RepositoryRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public RepositoryRights? RepositoryRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.ChatBotRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public ChatBotRights? ChatBotRights { get; set; }

		/// <summary>
		/// The <see cref="Api.Rights.ConfigurationRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		public ConfigurationRights? ConfigurationRights { get; set; }
	}
}
