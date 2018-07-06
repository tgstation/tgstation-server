using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Parameters necessary for duplicating a <see cref="ISessionController"/> session
	/// </summary>
	public sealed class ReattachInformation : ReattachInformationBase
	{
		/// <summary>
		/// The <see cref="IDmbProvider"/> used by DreamDaemon
		/// </summary>
		public IDmbProvider Dmb { get; set; }

		public ReattachInformation() { }
		public ReattachInformation(Models.ReattachInformation copy, IDmbFactory dmbFactory) : base(copy)
		{
			Dmb = dmbFactory.FromCompileJob(copy.CompileJob);
		}
	}
}