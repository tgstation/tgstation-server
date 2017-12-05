using System;

namespace TGS.Interface
{
	/// <summary>
	/// Main <see langword="interface"/> for communicating the <see cref="ITGSService"/>
	/// </summary>
	public interface IServerInterface : IDisposable, IRemoteConnectionProvider
	{
		/// <summary>
		/// The <see cref="Version"/> of the connected <see cref="ITGSService"/>
		/// </summary>
		Version ServerVersion { get; }

		/// <summary>
		/// Returns <see langword="true"/> if the <see cref="IServerInterface"/> interface being used to connect to a service does not have the same release version as the service
		/// </summary>
		/// <param name="errorMessage">An error message to display to the user should this function return <see langword="true"/></param>
		/// <returns><see langword="true"/> if the <see cref="IServerInterface"/> interface being used to connect to a service does not have the same release version as the service</returns>
		bool VersionMismatch(out string errorMessage);

		/// <summary>
		/// Returns the requested <see cref="IServerInterface"/> component <see langword="interface"/> for the instance <see cref="InstanceName"/>. This does not guarantee a successful connection.
		/// </summary>
		/// <typeparam name="T">The component <see langword="interface"/> to retrieve</typeparam>
		/// <returns>The correct component <see langword="interface"/></returns>
		T GetComponent<T>() where T : class;

		/// <summary>
		/// Returns a root service component
		/// </summary>
		/// <returns>The <see cref="ITGSService"/> component for the service</returns>
		T GetServiceComponent<T>() where T : class;
	}
}
