using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	public sealed class InstanceUser
    {
        /// <summary>
        /// See definition in <see cref="User.SystemIdentifier"/>
        /// </summary>
        public string SystemIdentifier { get; set; }

        /// <summary>
        /// The <see cref="ByondRights"/> of the <see cref="InstanceUser"/>
        /// </summary>
        public ByondRights ByondRights { get; set; }

        /// <summary>
        /// The <see cref="DreamDaemonRights"/> of the <see cref="InstanceUser"/>
        /// </summary>
        DreamDaemonRights DreamDaemonRights { get; set; }

        /// <summary>
        /// The <see cref="RepositoryRights"/> of the <see cref="InstanceUser"/>
        /// </summary>
        RepositoryRights RepositoryRights { get; set; }
    }
}
