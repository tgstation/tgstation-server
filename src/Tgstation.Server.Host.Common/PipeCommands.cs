using System.Collections.Generic;

namespace Tgstation.Server.Host.Common
{
	/// <summary>
	/// Values able to be passed via the update file path.
	/// </summary>
	public static class PipeCommands
	{
		/// <summary>
		/// Stops the server ASAP, shutting down any running instances.
		/// </summary>
		public const string CommandStop = "stop";

		/// <summary>
		/// Stops the server eventually, waiting for the games in any running instances to reboot.
		/// </summary>
		public const string CommandGracefulShutdown = "graceful";

		/// <summary>
		/// Stops the server ASAP, detaching the watchdog for any running instances.
		/// </summary>
		public const string CommandDetachingShutdown = "detach";

		/// <summary>
		/// All of the <see cref="PipeCommands"/> represented as a <see cref="IReadOnlyCollection{T}"/>.
		/// </summary>
		public static IReadOnlyList<string> AllCommands { get; } = new[]
		{
			CommandStop,
			CommandGracefulShutdown,
			CommandDetachingShutdown,
		};
	}
}
