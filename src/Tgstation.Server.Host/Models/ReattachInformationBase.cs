using System;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Base class for <see cref="ReattachInformation"/>
	/// </summary>
	public abstract class ReattachInformationBase : JsonSubFileList
	{
		/// <summary>
		/// Used to identify and authenticate the DreamDaemon instance
		/// </summary>
		[Required]
		public string AccessIdentifier { get; set; }

		/// <summary>
		/// The system process ID
		/// </summary>
		public int ProcessId { get; set; }

		/// <summary>
		/// If the <see cref="Components.IDmbProvider.PrimaryDirectory"/> of the associated dmb is being used
		/// </summary>
		public bool IsPrimary { get; set; }

		/// <summary>
		/// The port DreamDaemon was last listening on
		/// </summary>
		public ushort Port { get; set; }

		/// <summary>
		/// The current DreamDaemon reboot state
		/// </summary>
		public RebootState RebootState { get; set; }

		/// <summary>
		/// Construct a <see cref="ReattachInformationBase"/>
		/// </summary>
		protected ReattachInformationBase() { }

		/// <summary>
		/// Construct a <see cref="ReattachInformationBase"/> from a given <paramref name="copy"/>
		/// </summary>
		/// <param name="copy">The <see cref="ReattachInformationBase"/> to copy values from</param>
		protected ReattachInformationBase(ReattachInformationBase copy) : base(copy)
		{
			if (copy == null)
				throw new ArgumentNullException(nameof(copy));
			AccessIdentifier = copy.AccessIdentifier;
			IsPrimary = copy.IsPrimary;
			Port = copy.Port;
			ProcessId = copy.ProcessId;
			RebootState = copy.RebootState;
		}

		/// <inheritdoc />
		public override string ToString() => String.Format(CultureInfo.InvariantCulture, "Process ID: {3}, Access Identifier {4}, Primary: {0}, RebootState: {1}, Port: {2}", IsPrimary, RebootState, Port, ProcessId, AccessIdentifier);
	}
}
