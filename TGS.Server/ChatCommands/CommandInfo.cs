using TGS.Server.Components;

namespace TGS.Server.ChatCommands
{
	/// <summary>
	/// Metadata about the currently running <see cref="ChatCommand"/>
	/// </summary>
	sealed class CommandInfo
	{
		/// <summary>
		/// If the <see cref="ChatCommand"/> was invoked by an admin
		/// </summary>
		public bool IsAdmin { get; set; }
		/// <summary>
		/// If the <see cref="ChatCommand"/> was invoked from an admin chat channel
		/// </summary>
		public bool IsAdminChannel { get; set; }
		/// <summary>
		/// The name of the <see cref="ChatCommand"/> invoker
		/// </summary>
		public string Speaker { get; set; }
		/// <summary>
		/// A reference an <see cref="IByondManager"/> to check version information
		/// </summary>
		public IByondManager Byond { get; set; }
		/// <summary>
		/// A reference an <see cref="IInteropManager"/> to talk to the world
		/// </summary>
		public IInteropManager Interop { get; set; }
		/// <summary>
		/// A reference an <see cref="IInstanceLogger"/> to log messages
		/// </summary>
		public IInstanceLogger Logger { get; set; }
	}
}
