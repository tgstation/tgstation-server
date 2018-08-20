using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class InstanceUser : Api.Models.InstanceUser
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.Instance.Id"/> of <see cref="Instance"/>
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the <see cref="InstanceUser"/> belongs to
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <summary>
		/// If the <see cref="InstanceUser"/> has any instance rights
		/// </summary>
		public bool AnyRights => ByondRights != Api.Rights.ByondRights.None ||
			ChatBotRights != Api.Rights.ChatBotRights.None ||
			ConfigurationRights != Api.Rights.ConfigurationRights.None ||
			DreamDaemonRights != Api.Rights.DreamDaemonRights.None ||
			DreamMakerRights != Api.Rights.DreamMakerRights.None ||
			InstanceUserRights != Api.Rights.InstanceUserRights.None;

		/// <summary>
		/// Convert the <see cref="InstanceUser"/> to it's API form
		/// </summary>
		/// <returns>A new <see cref="Api.Models.InstanceUser"/></returns>
		public Api.Models.InstanceUser ToApi() => new Api.Models.InstanceUser
		{
			ByondRights = ByondRights,
			ChatBotRights = ChatBotRights,
			ConfigurationRights = ConfigurationRights,
			DreamDaemonRights = DreamDaemonRights,
			DreamMakerRights = DreamMakerRights,
			RepositoryRights = RepositoryRights,
			InstanceUserRights = InstanceUserRights,
			UserId = UserId
		};
	}
}
