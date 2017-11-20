using System;
using System.ServiceModel;
using SimpleInjector;
using SimpleInjector.Integration.Wcf;

namespace TGS.Server
{
	/// <inheritdoc />
	sealed class DependencyInjector : IDependencyInjector
	{
		/// <summary>
		/// The backing <see cref="Container"/>
		/// </summary>
		readonly Container container;
		
		/// <summary>
		/// Construct a <see cref="DependencyInjector"/>
		/// </summary>
		public DependencyInjector()
		{
			container = new Container();
			container.Options.DefaultLifestyle = Lifestyle.Singleton;
		}

		/// <inheritdoc />
		public T GetComponent<T>() where T : class
		{
			return container.GetInstance<T>();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			container.Dispose();
		}

		/// <inheritdoc />
		public ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses)
		{
			return new SimpleInjectorServiceHost(container, container.GetInstance(serviceType), baseAddresses);
		}

		/// <inheritdoc />
		public T GetInstance<T>() where T : class
		{
			return container.GetInstance<T>();
		}

		/// <inheritdoc />
		public void Register<T>(T instance) where T : class
		{
			container.RegisterSingleton(instance);
		}

		/// <inheritdoc />
		public void Register<TInterface, TImplementation>()
			where TInterface : class
			where TImplementation : class, TInterface
		{
			container.Register<TInterface, TImplementation>();
		}

		/// <inheritdoc />
		public void Setup()
		{
			container.Verify();
		}
	}
}
