namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Represents a dream daemon process
	/// </summary>
	interface ISession : ISessionBase
	{
		/// <summary>
		/// The <see cref="System.Diagnostics.Process.Id"/>
		/// </summary>
		int ProcessId { get; }

		/// <summary>
		/// Terminates the running process
		/// </summary>
		void Terminate();
	}
}