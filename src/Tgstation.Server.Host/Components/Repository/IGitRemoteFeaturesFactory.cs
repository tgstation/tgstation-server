using System;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Factory for creating <see cref="IGitRemoteFeatures"/>.
	/// </summary>
	interface IGitRemoteFeaturesFactory
	{
		/// <summary>
		/// Create the <see cref="IGitRemoteFeatures"/> for a given <paramref name="repository"/>.
		/// </summary>
		/// <param name="repository">The <see cref="IRepository"/> to create <see cref="IGitRemoteFeatures"/> for.</param>
		/// <returns>A new <see cref="IGitRemoteFeatures"/> instance.</returns>
		IGitRemoteFeatures CreateGitRemoteFeatures(IRepository repository);

		/// <summary>
		/// Gets the <see cref="RemoteGitProvider"/> for a given <paramref name="origin"/>.
		/// </summary>
		/// <param name="origin">The <see cref="Uri"/> of the origin.</param>
		/// <returns>The <see cref="RemoteGitProvider"/> of the <paramref name="origin"/>.</returns>
		RemoteGitProvider ParseRemoteGitProviderFromOrigin(Uri origin);
	}
}
