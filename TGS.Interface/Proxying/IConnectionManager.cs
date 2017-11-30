using System;

namespace TGS.Interface.Proxying
{
	interface IConnectionManager : IRemoteConnectionProvider, IDisposable
	{
		/// <summary>
		/// Returns the requested <see cref="ServerInterface"/> component <see langword="interface"/> for the instance <see cref="InstanceName"/>. This does not guarantee a successful connection. <see cref="ChannelFactory{TChannel}"/>s created this way are recycled for minimum latency and bandwidth usage
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> to retrieve</typeparam>
		/// <returns>The correct component <see langword="interface"/></returns>
		T GetComponent<T>() where T: class;
	}
}
