﻿using System;
using System.ServiceModel;

namespace TGS.Server.IoC
{
	/// <summary>
	/// Used for injecting <see cref="Components"/>
	/// </summary>
	interface IDependencyInjector : IDisposable
	{
		/// <summary>
		/// Creates a <see cref="ServiceHost"/> based on the <see cref="IDependencyInjector"/>
		/// </summary>
		/// <param name="serviceType">The type of the service</param>
		/// <param name="baseAddresses">The value of <see cref="ServiceHostBase.BaseAddresses"/></param>
		/// <returns>A new <see cref="ServiceHost"/></returns>
		ServiceHost CreateServiceHost(Type serviceType, Uri[] baseAddresses);

		/// <summary>
		/// Retrieve an implementation of <typeparamref name="T"/> from the <see cref="IDependencyInjector"/>
		/// </summary>
		/// <typeparam name="T">The type to retrieve</typeparam>
		/// <returns>An implementation of <typeparamref name="T"/></returns>
		T GetComponent<T>() where T : class;

		/// <summary>
		/// Register a singleton <paramref name="instance"/> of <typeparamref name="T"/>
		/// </summary>
		/// <typeparam name="T">The type to register</typeparam>
		/// <param name="instance">The implementation of <typeparamref name="T"/> to register</param>
		void Register<T>(T instance) where T : class;

		/// <summary>
		/// Register a <typeparamref name="TImplementation"/> of <typeparamref name="TInterface"/>
		/// </summary>
		/// <typeparam name="TInterface">The type to register</typeparam>
		/// <typeparam name="TImplementation">The implementation of <typeparamref name="TInterface"/> to register</typeparam>
		void Register<TInterface, TImplementation>() 
			where TInterface : class
			where TImplementation : class, TInterface;

		/// <summary>
		/// Validates the integrity of the <see cref="IDependencyInjector"/>
		/// </summary>
		void Setup();
	}
}
