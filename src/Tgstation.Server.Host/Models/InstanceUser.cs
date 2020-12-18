using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class InstanceUser : Api.Models.InstanceUser, IApiTransformable<Api.Models.InstanceUser>
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="Instance"/>
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the <see cref="InstanceUser"/> belongs to
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <inheritdoc />
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
