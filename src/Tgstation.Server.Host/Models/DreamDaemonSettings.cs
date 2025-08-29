namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class DreamDaemonSettings : Api.Models.Internal.DreamDaemonSettings
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>.
		/// </summary>
		public Instance Instance { get; set; } = null!; // recommended by EF
	}
}
