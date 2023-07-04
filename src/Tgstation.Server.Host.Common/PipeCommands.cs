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

#if NET6_0_OR_GREATER
		/// <summary>
		/// All of the <see cref="PipeCommands"/> represented as a <see cref="System.Collections.Generic.IReadOnlyList{T}"/>.
		/// </summary>
		public static System.Collections.Generic.IReadOnlyList<string> AllCommands { get; } = new[]
		{
			CommandStop,
			CommandGracefulShutdown,
			CommandDetachingShutdown,
		};
#endif

		/// <summary>
		/// Gets the <see cref="int"/> value of a given <paramref name="command"/>.
		/// </summary>
		/// <param name="command">The <see cref="PipeCommands"/>.</param>
		/// <returns>The <see cref="int"/> value of the command or <see langword="null"/> if it was unrecognized.</returns>
		public static int? GetCommandId(string command)
			=> command switch
			{
				CommandStop => 128, // Windows only allows commands 128-256: https://stackoverflow.com/a/62858106
				CommandGracefulShutdown => 129,
				CommandDetachingShutdown => 130,
				_ => null,
			};
	}
}
