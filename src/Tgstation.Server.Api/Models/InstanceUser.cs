using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a <see cref="User"/>s permissions in an <see cref="Instance"/>
	/// </summary>
	public class InstanceUser
	{
		/// <summary>
		/// The <see cref="Internal.User.Id"/> of the <see cref="User"/> the <see cref="InstanceUser"/> belongs to
		/// </summary>
		public long UserId { get; set; }

		/// <summary>
		/// The <see cref="Rights.InstanceUserRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		[Required]
		public InstanceUserRights? InstanceUserRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.ByondRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		[Required]
		public ByondRights? ByondRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.DreamDaemonRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		[Required]
		public DreamDaemonRights? DreamDaemonRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.DreamMakerRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		[Required]
		public DreamMakerRights? DreamMakerRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.RepositoryRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		[Required]
		public RepositoryRights? RepositoryRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.ChatBotRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		[Required]
		public ChatBotRights? ChatBotRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.ConfigurationRights"/> of the <see cref="InstanceUser"/>
		/// </summary>
		[Required]
		public ConfigurationRights? ConfigurationRights { get; set; }
	}
}
