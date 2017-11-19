namespace TGS.Server
{
	/// <summary>
	/// <see langword="interface"/> for providing <see cref="IRepoConfig"/>s
	/// </summary>
	interface IRepoConfigProvider
	{
		/// <summary>
		/// Create a <see cref="IRepoConfig"/>
		/// </summary>
		/// <returns>A new <see cref="IRepoConfig"/> based on provider parameters</returns>
		IRepoConfig GetRepoConfig();
	}
}
