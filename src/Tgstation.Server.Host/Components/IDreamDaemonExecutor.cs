using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For creating <see cref="IDreamDaemonSession"/>s
	/// </summary>
    interface IDreamDaemonExecutor
    {
		/// <summary>
		/// Run a dream daemon instance
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/></param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> that is completed when dream daemon starts without crashing</param>
		/// <param name="dreamDaemonPath">The path to the dreamdaemon executable</param>
		/// <param name="dmbProvider">The <see cref="IDmbProvider"/> for the .dmb to run</param>
		/// <param name="parameters">The value of the -params command line option</param>
		/// <param name="useSecondaryPort">If the <see cref="DreamDaemonLaunchParameters.SecondaryPort"/> field of <paramref name="launchParameters"/> should be used</param>
		/// <param name="useSecondaryDirectory">If the <see cref="IDmbProvider.SecondaryDirectory"/> field of <paramref name="dmbProvider"/> should be used</param>
		/// <returns>A new <see cref="IDreamDaemonSession"/></returns>
		IDreamDaemonSession RunDreamDaemon(DreamDaemonLaunchParameters launchParameters, string dreamDaemonPath, IDmbProvider dmbProvider, string parameters, bool useSecondaryPort, bool useSecondaryDirectory);

		/// <summary>
		/// Attach to a running instance of DreamDaemon
		/// </summary>
		/// <param name="processId">The <see cref="IDreamDaemonSession.ProcessId"/></param>
		/// <returns>A new <see cref="IDreamDaemonSession"/></returns>
		IDreamDaemonSession AttachToDreamDaemon(int processId);
	}
}
