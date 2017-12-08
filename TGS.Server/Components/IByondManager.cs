using TGS.Interface.Components;

namespace TGS.Server.Components
{
	/// <summary>
	/// Interface for managing a BYOND installation
	/// </summary>
	interface IByondManager : ITGByond
	{
		/// <summary>
		/// Attempts to change the installation's <see cref="ITGByond.CurrentStatus"/> from <see cref="Interface.ByondStatus.Staged"/> to <see cref="Interface.ByondStatus.Updating"/> and then to <see cref="Interface.ByondStatus.Idle"/>
		/// </summary>
		/// <returns><see langword="true"/> on success, <see langword="false"/> on failure</returns>
		bool ApplyStagedUpdate();

		/// <summary>
		/// Get the path to dm.exe and prevent the installation from being modified until <see cref="UnlockDMExecutable()"/> is called with the return value of this function
		/// </summary>
		/// <param name="useStagedIfPossible">Use the staged installation if it exists</param>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <returns>The full path to the specified executable on success, <see langword="null"/> on failure</returns>
		string LockDMExecutable(bool useStagedIfPossible, out string error);

		/// <summary>
		/// Get the path to dreamdaemon.exe and prevent the installation from being modified until <see cref="UnlockDDExecutable()"/> is called with the return value of this function
		/// </summary>
		/// <param name="error"><see langword="null"/> on success, error message on failure</param>
		/// <returns>The full path to the installed dreamdaemon.exe on success, <see langword="null"/> on failure</returns>
		string LockDDExecutable(out string error);

		/// <summary>
		/// Unlocks an executable retrieved from <see cref="LockDMExecutable(bool, out string)"/>. That path must not be used again after calling this function
		/// </summary>
		void UnlockDMExecutable();

		/// <summary>
		/// Unlocks an executable retrieved from <see cref="LockDDExecutable(out string)"/>. That path must not be used again after calling this function
		/// </summary>
		void UnlockDDExecutable();

		/// <summary>
		/// Attempts to clear the BYOND cache folder in Documents
		/// </summary>
		void ClearCache();
	}
}
