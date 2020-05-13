using System;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Base class for <see cref="DualReattachInformation"/>
	/// </summary>
	public abstract class DualReattachInformationBase
	{
		/// <summary>
		/// If the Alpha session is the active session
		/// </summary>
		public bool AlphaIsActive { get; set; }

		/// <summary>
		/// Construct a <see cref="DualReattachInformationBase"/>
		/// </summary>
		public DualReattachInformationBase() { }

		/// <summary>
		/// Construct a <see cref="DualReattachInformation"/> from a given <paramref name="copy"/>
		/// </summary>
		/// <param name="copy">The <see cref="DualReattachInformationBase"/> to copy values from</param>
		protected DualReattachInformationBase(DualReattachInformationBase copy)
		{
			AlphaIsActive = copy?.AlphaIsActive ?? throw new ArgumentNullException(nameof(copy));
		}
	}
}
