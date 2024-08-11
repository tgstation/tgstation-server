namespace Tgstation.Server.Host.Configuration
{
	/// <summary>
	/// Configuration options for the game sessions.
	/// </summary>
	sealed class SessionConfiguration
	{
		/// <summary>
		/// The key for the <see cref="Microsoft.Extensions.Configuration.IConfigurationSection"/> the <see cref="SessionConfiguration"/> resides in.
		/// </summary>
		public const string Section = "Session";

		/// <summary>
		/// The default value for <see cref="HighPriorityLiveDreamDaemon"/>.
		/// </summary>
		const bool DefaultHighPriorityLiveDreamDaemon = true;

		/// <summary>
		/// If the public DreamDaemon instances are set to be above normal priority processes.
		/// </summary>
		public bool HighPriorityLiveDreamDaemon { get; set; } = DefaultHighPriorityLiveDreamDaemon;

		/// <summary>
		/// If the deployment DreamMaker and DreamDaemon instances are set to be below normal priority processes.
		/// </summary>
		public bool LowPriorityDeploymentProcesses { get; set; }

		/// <summary>
		/// If <see langword="true"/>, deployments that fail will not be immediately cleaned up. They will be cleaned up the next time the instance is onlined.
		/// </summary>
		public bool DelayCleaningFailedDeployments { get; set; }

		/// <summary>
		/// If set dd.exe will not be used on Windows systems in versions where it is present. Instead dreamdaemon.exe will always be used.
		/// </summary>
		public bool ForceUseDreamDaemonExe { get; set; }
	}
}
