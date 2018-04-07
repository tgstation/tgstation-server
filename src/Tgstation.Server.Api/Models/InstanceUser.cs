using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a <see cref="User"/>s permissions in an <see cref="Instance"/>
	/// </summary>
	[Model(RightsType.InstanceUser, WriteRight = InstanceUserRights.WriteUsers, CanList = true, RequiresInstance = true)]
	public sealed class InstanceUser
    {
        /// <summary>
        /// See definition in <see cref="User.Id"/>
        /// </summary>
		[Permissions(DenyWrite = true)]
        public long Id { get; set; }

		/// <summary>
		/// The <see cref="Rights.ByondRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		public ByondRights ByondRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.DreamDaemonRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		public DreamDaemonRights DreamDaemonRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.DreamMakerRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		public DreamMakerRights DreamMakerRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.RepositoryRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		public RepositoryRights RepositoryRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.ChatSettingsRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		public ChatSettingsRights ChatRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.ConfigurationRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		public ConfigurationRights ConfigurationRights { get; set; }
	}
}
