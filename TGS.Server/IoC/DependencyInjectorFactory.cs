namespace TGS.Server.IoC
{
	/// <inheritdoc />
	sealed class DependencyInjectorFactory : IDependencyInjectorFactory
	{
		/// <inheritdoc />
		public IDependencyInjector CreateDependencyInjector()
		{
			return new DependencyInjector();
		}
	}
}
