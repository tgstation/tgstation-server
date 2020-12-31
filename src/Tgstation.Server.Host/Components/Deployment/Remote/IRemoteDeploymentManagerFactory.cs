using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Deployment.Remote
{
	/// <summary>
	/// Factory for creating <see cref="IRemoteDeploymentManager"/>s.
	/// </summary>
	interface IRemoteDeploymentManagerFactory
	{
		/// <summary>
		/// Creates a <see cref="IRemoteDeploymentManager"/> for a given <paramref name="remoteGitProvider"/>.
		/// </summary>
		/// <param name="metadata">Current <see cref="Api.Models.Instance"/> metadata.</param>
		/// <param name="remoteGitProvider">The <see cref="RemoteGitProvider"/> in use.</param>
		/// <returns>A new <see cref="IRemoteDeploymentManager"/> based on the <paramref name="remoteGitProvider"/>.</returns>
		IRemoteDeploymentManager CreateRemoteDeploymentManager(Api.Models.Instance metadata, RemoteGitProvider remoteGitProvider);

		/// <summary>
		/// Create a <see cref="IRemoteDeploymentManager"/> for a given <paramref name="compileJob"/>.
		/// </summary>
		/// <param name="metadata">Current <see cref="Api.Models.Instance"/> metadata.</param>
		/// <param name="compileJob">The <see cref="CompileJob"/> containing the <see cref="CompileJob.RepositoryOrigin"/> to use.</param>
		/// <returns>A new <see cref="IRemoteDeploymentManager"/>.</returns>
		IRemoteDeploymentManager CreateRemoteDeploymentManager(Api.Models.Instance metadata, Models.CompileJob compileJob);
	}
}
