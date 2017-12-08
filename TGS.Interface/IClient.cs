using System;

namespace TGS.Interface
{
	/// <summary>
	/// Main <see langword="interface"/> for communicating the <see cref="Components.ITGSService"/>
	/// </summary>
	public interface IClient : IDisposable
	{
		/// <summary>
		/// The <see cref="IServer"/> the <see cref="IClient"/> connects to
		/// </summary>
		IServer Server { get; }

		/// <summary>
		/// The <see cref="RemoteLoginInfo"/> for the <see cref="IClient"/>. Is <see langword="null"/> for local connections
		/// </summary>
		RemoteLoginInfo LoginInfo { get; }

		/// <summary>
		/// Checks if the <see cref="IClient"/> is setup for a remote connection
		/// </summary>
		bool IsRemoteConnection { get; }

		/// <summary>
		/// Returns <see langword="true"/> if the <see cref="IClient"/> interface being used to connect to a service does not have the same release version as the service
		/// </summary>
		/// <param name="errorMessage">An error message to display to the user should this function return <see langword="true"/></param>
		/// <returns><see langword="true"/> if the <see cref="IClient"/> interface being used to connect to a service does not have the same release version as the service</returns>
		bool VersionMismatch(out string errorMessage);

		/// <summary>
		/// See <see cref="ConnectionStatus(out string)"/> without the error argument
		/// </summary>
		ConnectivityLevel ConnectionStatus();

		/// <summary>
		/// Used to test if the <see cref="Components.ITGSService"/> is avaiable on the target machine. Note that state can change at any time and any call into the may throw an exception because of communcation errors
		/// </summary>
		/// <param name="error">String of the error that prevented an elevated connectivity level</param>
		/// <returns>The apporopriate <see cref="ConnectivityLevel"/></returns>
		ConnectivityLevel ConnectionStatus(out string error);
	}
}
