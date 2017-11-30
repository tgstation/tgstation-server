using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
		/// The name of the current instance in use. Defaults to <see langword="null"/>
		/// </summary>
		string InstanceName { get; }

		/// <summary>
		/// Targets <paramref name="instanceName"/> as the instance to use with <see cref="GetComponent{T}"/>. Closes all connections to any previous instance
		/// </summary>
		/// <param name="instanceName">The name of the instance to connect to</param>
		/// <param name="skipChecks">If set to <see langword="true"/>, skips the connectivity and authentication checks, sets <see cref="InstanceName"/>, and returns <see cref="ConnectivityLevel.Connected"/></param>
		/// <returns>The apporopriate <see cref="ConnectivityLevel"/></returns>
		ConnectivityLevel ConnectToInstance(string instanceName = null, bool skipChecks = false);

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

		/// <summary>
		/// Used to test if the <see cref="ITGSService"/> is avaiable on the target machine. Note that state can change at any time and any call into the may throw an exception because of communcation errors
		/// </summary>
		/// <returns><see langword="null"/> on successful connection, error message <see cref="string"/> on failure</returns>
		ConnectivityLevel ConnectionStatus();

		/// <summary>
		/// Used to test if the <see cref="ITGSService"/> is avaiable on the target machine. Note that state can change at any time and any call into the may throw an exception because of communcation errors
		/// </summary>
		/// <param name="error">String of the error that prevented an elevated connectivity level</param>
		/// <returns>The apporopriate <see cref="ConnectivityLevel"/></returns>
		ConnectivityLevel ConnectionStatus(out string error);
	}
}
