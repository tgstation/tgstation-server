namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Database representation of <see cref="Components.Session.DualReattachInformation"/>
	/// </summary>
	public sealed class DualReattachInformation : DualReattachInformationBase
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of the <see cref="Instance"/> the <see cref="DualReattachInformation"/> belongs to
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The <see cref="ReattachInformation.Id"/> of <see cref="Alpha"/>.
		/// </summary>
		public long? AlphaId { get; set; }

		/// <summary>
		/// The <see cref="ReattachInformation"/> for the Alpha server
		/// </summary>
		public ReattachInformation Alpha { get; set; }

		/// <summary>
		/// The <see cref="ReattachInformation.Id"/> of <see cref="Bravo"/>.
		/// </summary>
		public long? BravoId { get; set; }

		/// <summary>
		/// The <see cref="ReattachInformation"/> for the Bravo server
		/// </summary>
		public ReattachInformation Bravo { get; set; }
	}
}
