using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a <see cref="User"/>s permissions in an <see cref="Instance"/>
	/// </summary>
	[Model(RightsType.InstanceUser, WriteRight = InstanceUserRights.WriteUsers)]
	public sealed class InstanceUser
    {
        /// <summary>
        /// See definition in <see cref="User.Id"/>
        /// </summary>
		[Permissions(DenyWrite = true)]
        public long Id { get; set; }

        /// <summary>
        /// The <see cref="ByondRights"/> of the <see cref="InstanceUser"/>
        /// </summary>
        public ByondRights ByondRights { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		public DreamDaemonRights DreamDaemonRights { get; set; }

		/// <summary>
		/// The <see cref="RepositoryRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		public RepositoryRights RepositoryRights { get; set; }
    }
}
