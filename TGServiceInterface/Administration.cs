﻿using System.ServiceModel;

namespace TGServiceInterface
{
	/// <summary>
	/// Manage the group that is used to access the service, can only be used by an administrator
	/// </summary>
	[ServiceContract]
	public interface ITGAdministration
	{
        /// <summary>
        /// Check and see which URL is being used to load the remote access certificate
        /// e.g. a value of localhost will load the IIS Development Certificate
        /// </summary>
        /// <returns>The URL indicating which URL is being used to find the certificate</returns>
        [OperationContract]
        string RemoteURL();

        /// <summary>
        /// Set the URL used to find and load the remote access certificate
        /// Requires a service restart to take effect
        /// </summary>
        /// <param name="URL">The URL to use for lookup</param>
        [OperationContract]
        void SetRemoteURL(string URL);

		/// <summary>
		/// Get the port used for remote operation
		/// </summary>
		/// <returns>The port used for remote operation</returns>
		[OperationContract]
		ushort RemoteAccessPort();

		/// <summary>
		/// Set the port used for remote operation
		/// Requires a service restart to take effect
		/// </summary>
		/// <param name="port">The new port to use for remote operation</param>
		/// <returns>null on success, error message on failure</returns>
		[OperationContract]
		string SetRemoteAccessPort(ushort port);

		/// <summary>
		/// Returns the name of the windows group allowed to use the service other than administrator
		/// </summary>
		/// <returns>The name of the windows group allowed to use the service other than administrator, "ADMIN" if it's unset, null on failure</returns>
		[OperationContract]
		string AuthorizedGroup();

		/// <summary>
		/// Searches the windows machine for the group named <paramref name="groupName"/>, sets it as the authorized group if it's found
		/// </summary>
		/// <param name="groupName">The name of the windows group to search for or null to clear the setting</param>
		/// <returns>The name of the windows group that is now authorized to use the service on success, null on failure, "ADMIN" on clearing</returns>
		[OperationContract]
		string SetAuthorizedGroup(string groupName);
	}
}
