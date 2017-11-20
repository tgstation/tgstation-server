namespace TGS.Server
{
	/// <inheritdoc />
	class DependencyInjectorFactory : IDependencyInjectorFactory
	{
		/// <inheritdoc />
		public IDependencyInjector CreateDependencyInjector()
		{
			return new DependencyInjector();
		}
	}
}
