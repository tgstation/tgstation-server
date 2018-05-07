using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

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
		public bool AnyRights => ByondRights != ByondRights.None ||
			ChatSettingsRights != ChatSettingsRights.None ||
			ConfigurationRights != ConfigurationRights.None ||
			DreamDaemonRights != DreamDaemonRights.None ||
			DreamMakerRights != DreamMakerRights.None;
	}
}
