using System;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Api.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="JobCode"/> <see langword="enum"/>.
	/// </summary>
	public static class JobCodeExtensions
	{
		/// <summary>
		/// If a given <paramref name="jobCode"/> can be triggered by TGS startup.
		/// </summary>
		/// <param name="jobCode">The <see cref="JobCode"/>.</param>
		/// <returns><see langword="true"/> if the <see cref="JobCode"/> can trigger before startup, <see langword="false"/> otherwise.</returns>
		public static bool IsServerStartupJob(this JobCode jobCode)
			=> jobCode switch
			{
				JobCode.Unknown or JobCode.Move or JobCode.RepositoryClone or JobCode.RepositoryUpdate or JobCode.RepositoryAutoUpdate or JobCode.RepositoryDelete or JobCode.EngineOfficialInstall or JobCode.EngineCustomInstall or JobCode.EngineDelete or JobCode.Deployment or JobCode.AutomaticDeployment or JobCode.WatchdogLaunch or JobCode.WatchdogRestart or JobCode.WatchdogDump => false,
				JobCode.StartupWatchdogLaunch or JobCode.StartupWatchdogReattach or JobCode.ReconnectChatBot => true,
				_ => throw new InvalidOperationException($"Invalid JobCode: {jobCode}"),
			};
	}
}
