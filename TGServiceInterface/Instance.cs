using System.ServiceModel;

namespace TGServiceInterface
{

	/// <summary>
	/// How to modify the repo during the UpdateServer operation
	/// </summary>
	public enum TGRepoUpdateMethod
	{
		/// <summary>
		/// Do not update the repo
		/// </summary>
		None,
		/// <summary>
		/// Update the repo by merging the origin branch
		/// </summary>
		Merge,
		/// <summary>
		/// Update the repo by hard resetting to the remote branch
		/// </summary>
		Hard,
		/// <summary>
		/// Clean the repo by hard resetting to the origin branch
		/// </summary>
		Reset,
	}

	/// <summary>
	/// Manage a server instance
	/// </summary>
	[ServiceContract]
	public interface ITGInstance
	{
		/// <summary>
		/// Gets the instance's name
		/// </summary>
		/// <returns>The instance's name</returns>
		[OperationContract]
		string InstanceName();

		/// <summary>
		/// Gets the instance's ID
		/// </summary>
		/// <returns></returns>
		[OperationContract]
		int InstanceID();

		/// <summary>
		/// Updates the server fully with various options as a blocking operation
		/// </summary>
		/// <param name="updateType">How to handle the repository during the update</param>
		/// <param name="push_changelog_if_enabled">true if the changelog should be pushed to git</param>
		/// <param name="testmerge_pr">If not zero, will testmerge the designated pull request</param>
		/// <returns>null on success, error message on failure</returns>
		[OperationContract]
		string UpdateServer(TGRepoUpdateMethod updateType, bool push_changelog_if_enabled, ushort testmerge_pr = 0);

		/// <summary>
		/// Deletes the instance and everything it manages this will stop the server, cancel any pending repo actions and delete the instance's directory
		/// </summary>
		[OperationContract]
		void Delete();

		/// <summary>
		/// Return the directory of the server on the host machine
		/// </summary>
		/// <returns>The path to the directory on success, null on failure</returns>
		[OperationContract]
		string InstanceDirectory();

		/// <summary>
		/// Moves the entire server installation, requires no operations to be running
		/// </summary>
		/// <param name="new_location">The new path to place the server</param>
		/// <returns>null on success, error message on failure</returns>
		[OperationContract]
		string MoveInstance(string new_location);
	}
}
