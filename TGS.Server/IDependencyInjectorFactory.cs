namespace TGS.Server
{
	/// <summary>
	/// <see langword="interface"/> for creating a <see cref="IDependencyInjector"/>
	/// </summary>
	interface IDependencyInjectorFactory
	{
		/// <summary>
		/// Create a <see cref="IDependencyInjector"/>
		/// </summary>
		/// <returns>A new <see cref="IDependencyInjector"/></returns>
		IDependencyInjector CreateDependencyInjector();
	}
}
