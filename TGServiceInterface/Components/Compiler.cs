using System.ServiceModel;

namespace TGServiceInterface.Components
{
	/// <summary>
	/// For managing the Game A/B/Live folders, compiling, and hotswapping them
	/// </summary>
	[ServiceContract]
	public interface ITGCompiler
	{
		/// <summary>
		/// Sets up the symlinks for hotswapping game code. This will reset everything in the Game folder. Requires the repository to be set up and locks it once the compilation stage starts. Runs asyncronously from this call
		/// </summary>
		/// <returns><see langword="true"/> if the operation began, <see langword="false"/> if it could not start</returns>
		[OperationContract]
		bool Initialize();

		/// <summary>
		/// Does all the necessary actions to take the revision currently in the repository and compile it to be run on the next server reboot. Requires BYOND to be set up and the <see cref="GetStatus"/> to return <see cref="CompilerStatus.Initialized"/>. Runs asyncronously
		/// </summary>
		/// <param name="silent">If <see langword="true"/> no message for compilation start will be printed</param>
		/// <returns><see langword="true"/> if the operation began, <see langword="false"/> if it could not start</returns>
		[OperationContract]
		bool Compile(bool silent = false);

		/// <summary>
		/// Cancels the current compilation
		/// </summary>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		[OperationContract]
		string Cancel();

		/// <summary>
		/// Returns the current compiler status
		/// </summary>
		/// <returns>The current compiler status</returns>
		[OperationContract]
		CompilerStatus GetStatus();

		/// <summary>
		/// Returns the error message of the last operation. Reading this will clear the stored value
		/// </summary>
		/// <returns>the error message of the last operation if it failed or <see langword="null"/> if it succeeded</returns>
		[OperationContract]
		string CompileError();

		/// <summary>
		/// Returns the relative path of the dme the compiler will look for without the .dme part
		/// </summary>
		/// <returns>The relative path of the dme the compiler will look for without the .dme part</returns>
		[OperationContract]
		string ProjectName();

		/// <summary>
		/// Sets the relative path of the dme the compiler will look for without the .dme part
		/// </summary>
		/// <param name="projectName">The relative path of the dme the compiler will look for without the .dme part</param>
		[OperationContract]
		void SetProjectName(string projectName);
	}
}
