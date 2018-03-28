using System.ServiceModel;

namespace TGS.Interface.Components
{
	/// <summary>
	/// Metadata for a server instance
	/// </summary>
	[ServiceContract]
	public interface ITGInstance
	{
		/// <summary>
		/// Return the directory of the server on the host machine
		/// </summary>
		/// <returns>The path to the directory on success, null on failure</returns>
		[OperationContract]
		string ServerDirectory();

		/// <summary>
		/// Retrieve's the service's version
		/// </summary>
		/// <returns>The service's version</returns>
		[OperationContract]
		string Version();

		/// <summary>
		/// Updates the cached TGS3.json to the repo's version. Compiles will not succeed if these two to not match
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string UpdateTGS3Json();

		/// <summary>
		/// (De)Activate and set the interval for the automatic server updater
		/// </summary>
		/// <param name="newInterval">Interval to check for updates in minutes, disables if 0</param>
		[OperationContract]
		void SetAutoUpdateInterval(ulong newInterval);

		/// <summary>
		/// Get the current autoupdate interval
		/// </summary>
		/// <returns>The current auto update interval or 0 if it's disabled</returns>
		[OperationContract]
		ulong AutoUpdateInterval();
	}
}
