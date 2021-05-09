namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// On Windows, DreamDaemon will show an unskippable prompt when using /world/proc/OpenPort(). This looks out for those prompts and immediately clicks "Yes" if the owning process has registered for it.
	/// </summary>
	interface INetworkPromptReaper
	{
		/// <summary>
		/// Register a given <paramref name="process"/> for network prompt reaping.
		/// </summary>
		/// <param name="process">The <see cref="IProcess"/> to register.</param>
		void RegisterProcess(IProcess process);
	}
}
