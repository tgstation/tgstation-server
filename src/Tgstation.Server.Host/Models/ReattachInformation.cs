namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Database representation of <see cref="Components.Watchdog.ReattachInformation"/>
	/// </summary>
	public sealed class ReattachInformation : ReattachInformationBase
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Models.CompileJob"/> for the <see cref="Components.Watchdog.ReattachInformation.Dmb"/>
		/// </summary>
		public CompileJob CompileJob { get; set; }
	}
}
