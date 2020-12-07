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
	}
}
