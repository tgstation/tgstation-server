namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Provider for <see cref="IInstanceCore"/>s
	/// </summary>
	interface IInstanceCoreProvider
	{
		/// <summary>
		/// Get the <see cref="IInstanceCore"/> for a given <paramref name="instance"/> if it's online.
		/// </summary>
		/// <param name="instance">The <see cref="Instance"/> to get the <see cref="IInstanceCore"/> for.</param>
		/// <returns>The <see cref="IInstanceCore"/> if it is online, <see langword="null"/> otherwise.</returns>
		IInstanceCore GetInstance(Models.Instance instance);
	}
}
