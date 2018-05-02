using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// For running a DreamDaemon instance
	/// </summary>
    interface IDreamDaemonExecutor
    {
		/// <summary>
		/// Run a dream daemon instance
		/// </summary>
		/// <param name="launchParameters">The <see cref="DreamDaemonLaunchParameters"/></param>
		/// <param name="onSuccessfulStartup">The <see cref="TaskCompletionSource{TResult}"/> that is completed when dream daemon starts without crashing</param>
		/// <param name="dreamDaemonPath">The path to the dreamdaemon executable</param>
		/// <param name="dmbPath">The path to the .dmb to run, the working directory will be derived from this</param>
		/// <param name="accessToken">The access token to be used for communication</param>
		/// <param name="usePrimaryPort">If the server should open on <see cref="DreamDaemonLaunchParameters.PrimaryPort"/> or <see cref="DreamDaemonLaunchParameters.SecondaryPort"/> of <paramref name="launchParameters"/></param>
		/// <param name="alwaysKill">If the resulting process should never be left alive</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation</param>
		/// <returns>A <see cref="Task{TResult}"/> representing the lifetime of the process and resulting in the exit code</returns>
		Task<int> RunDreamDaemon(DreamDaemonLaunchParameters launchParameters, TaskCompletionSource<object> onSuccessfulStartup, string dreamDaemonPath, string dmbPath, string accessToken, bool usePrimaryPort, bool alwaysKill, CancellationToken cancellationToken);
	}
}
