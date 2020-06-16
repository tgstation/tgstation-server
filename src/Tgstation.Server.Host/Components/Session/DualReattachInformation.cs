using System;
using System.Globalization;
using Tgstation.Server.Host.Components.Deployment;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Session
{
	/// <summary>
	/// Reattach information for two <see cref="ISessionController"/>
	/// </summary>
	public sealed class DualReattachInformation : DualReattachInformationBase
	{
		/// <summary>
		/// <see cref="ReattachInformation"/> for the Alpha session
		/// </summary>
		public ReattachInformation Alpha { get; set; }

		/// <summary>
		/// <see cref="ReattachInformation"/> for the Bravo session
		/// </summary>
		public ReattachInformation Bravo { get; set; }

		/// <summary>
		/// The timeout used for topic request.
		/// </summary>
		public TimeSpan TopicRequestTimeout { get; set; }

		/// <summary>
		/// Construct a <see cref="DualReattachInformation"/>
		/// </summary>
		public DualReattachInformation() { }

		/// <summary>
		/// Construct a <see cref="DualReattachInformation"/> from a given <paramref name="copy"/> with a given <paramref name="dmbAlpha"/> and <paramref name="dmbBravo"/>
		/// </summary>
		/// <param name="copy">The <see cref="DualReattachInformationBase"/> to copy information from</param>
		/// <param name="dmbAlpha">The <see cref="IDmbProvider"/> used to build <see cref="Alpha"/></param>
		/// <param name="dmbBravo">The <see cref="IDmbProvider"/> used to build <see cref="Bravo"/></param>
		public DualReattachInformation(Models.DualReattachInformation copy, IDmbProvider dmbAlpha, IDmbProvider dmbBravo) : base(copy)
		{
			if (copy.Alpha != null)
				Alpha = new ReattachInformation(copy.Alpha, dmbAlpha);
			if (copy.Bravo != null)
				Bravo = new ReattachInformation(copy.Bravo, dmbBravo);
		}

		/// <inheritdoc />
		public override string ToString() => String.Format(
			CultureInfo.InvariantCulture,
			"Alpha: {0}, Bravo {1}",
			Alpha?.ToString() ?? "(null)",
			Bravo?.ToString() ?? "(null)");
	}
}
