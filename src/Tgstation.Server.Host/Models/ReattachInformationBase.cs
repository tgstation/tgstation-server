using System;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.Components.Session;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Base class for <see cref="ReattachInformation"/>.
	/// </summary>
	public abstract class ReattachInformationBase : DMApiParameters
	{
		/// <summary>
		/// The database row Id.
		/// </summary>
		public long? Id { get; set; }

		/// <summary>
		/// The system process ID.
		/// </summary>
		public int ProcessId { get; set; }

		/// <summary>
		/// The port the game server was last listening on.
		/// </summary>
		public ushort Port { get; set; }

		/// <summary>
		/// The port the game server was last listening on for topics.
		/// </summary>
		public ushort? TopicPort { get; set; }

		/// <summary>
		/// The current DreamDaemon reboot state.
		/// </summary>
		public RebootState RebootState { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonSecurity"/> level DreamDaemon was launched with.
		/// </summary>
		public DreamDaemonSecurity LaunchSecurityLevel { get; set; }

		/// <summary>
		/// The <see cref="DreamDaemonVisibility"/> DreamDaemon was launched with.
		/// </summary>
		public DreamDaemonVisibility LaunchVisibility { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformationBase"/> class.
		/// </summary>
		protected ReattachInformationBase()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ReattachInformationBase"/> class.
		/// </summary>
		/// <param name="copy">The <see cref="ReattachInformationBase"/> to copy values from.</param>
		protected ReattachInformationBase(ReattachInformationBase copy)
		{
			ArgumentNullException.ThrowIfNull(copy);
			Id = copy.Id;
			AccessIdentifier = copy.AccessIdentifier;
			Port = copy.Port;
			TopicPort = copy.TopicPort;
			ProcessId = copy.ProcessId;
			RebootState = copy.RebootState;
			LaunchSecurityLevel = copy.LaunchSecurityLevel;
			LaunchVisibility = copy.LaunchVisibility;
		}

		/// <inheritdoc />
		public override string ToString() => $"Session: {Id}, PID: {ProcessId}, Access Identifier {AccessIdentifier}, RebootState: {RebootState}, Port: {Port}";
	}
}
