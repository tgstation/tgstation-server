using System;
using System.Collections.Generic;
using TGS.Interface.Components;

namespace TGS.Interface.Wrappers
{
	/// <summary>
	/// Wrapper representing a <see cref="ITGSService"/>
	/// </summary>
	public interface IServer : ITGSService
	{
		/// <summary>
		/// The <see cref="System.Version"/> of the <see cref="IServer"/>
		/// </summary>
		new Version Version { get; }
		
		/// <summary>
		/// Get the <see cref="IInstance"/>s the <see cref="IServer"/> contains that the current user can access and connect to
		/// </summary>
		IEnumerable<IInstance> Instances { get; }

		/// <summary>
		/// The <see cref="ITGInstanceManager"/> component
		/// </summary>
		ITGInstanceManager InstanceManager { get; }

		/// <summary>
		/// Gets the specified <see cref="IInstance"/> without connectivity checks
		/// </summary>
		/// <param name="name">The name of the <see cref="IInstance"/> to get</param>
		/// <returns>The <see cref="IInstance"/> named <paramref name="name"/> on success, <see langword="null"/> on failure</returns>
		IInstance GetInstance(string name);
	}
}
