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
		/// The <see cref="Models.Instance"/> the <see cref="InstanceUser"/> belongs to
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <summary>
		/// If the <see cref="InstanceUser"/> has any instance rights
		/// </summary>
		public bool AnyRights => ByondRights != Api.Rights.ByondRights.None ||
			ChatSettingsRights != Api.Rights.ChatSettingsRights.None ||
			ConfigurationRights != Api.Rights.ConfigurationRights.None ||
			DreamDaemonRights != Api.Rights.DreamDaemonRights.None ||
			DreamMakerRights != Api.Rights.DreamMakerRights.None;
	}
}
