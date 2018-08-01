using System;
using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Host.Components.Watchdog;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Base class for <see cref="ReattachInformation"/>
	/// </summary>
	public abstract class ReattachInformationBase
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
		[Required]
		public RebootState RebootState { get; set; }

		/// <summary>
		/// Path to the chat commands json file
		/// </summary>
		[Required]
		public string ChatCommandsJson { get; set; }

		/// <summary>
		/// Path to the chat channels json file
		/// </summary>
		[Required]
		public string ChatChannelsJson { get; set; }

		/// <summary>
		/// Construct a <see cref="ReattachInformationBase"/>
		/// </summary>
		public ReattachInformationBase() { }

		/// <summary>
		/// Construct a <see cref="ReattachInformationBase"/> from a given <paramref name="copy"/>
		/// </summary>
		/// <param name="copy">The <see cref="ReattachInformationBase"/> to copy values from</param>
		protected ReattachInformationBase(ReattachInformationBase copy)
		{
			if (copy == null)
				throw new ArgumentNullException(nameof(copy));
			AccessIdentifier = copy.AccessIdentifier;
			ChatChannelsJson = copy.ChatChannelsJson;
			ChatCommandsJson = copy.ChatCommandsJson;
			IsPrimary = copy.IsPrimary;
			Port = copy.Port;
			ProcessId = copy.ProcessId;
			RebootState = copy.RebootState;
		}
	}
}
