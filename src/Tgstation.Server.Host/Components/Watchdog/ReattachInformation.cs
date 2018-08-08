using System;
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

		/// <summary>
		/// Construct a <see cref="ReattachInformation"/>
		/// </summary>
		public ReattachInformation() { }

		/// <summary>
		/// Construct a <see cref="ReattachInformation"/> from a given <paramref name="copy"/> and <paramref name="dmbFactory"/>
		/// </summary>
		/// <param name="copy">The <see cref="Models.ReattachInformation"/> to copy values from</param>
		/// <param name="dmb">The value of <see cref="Dmb"/></param>
		public ReattachInformation(Models.ReattachInformation copy, IDmbProvider dmb) : base(copy)
		{
			Dmb = dmb ?? throw new ArgumentNullException(nameof(dmb));
		}
	}
}