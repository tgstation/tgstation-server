namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// For deleting <see cref="Host"/> libraries used by the program
	/// </summary>
	interface IActiveLibraryDeleter
	{
		/// <summary>
		/// Deletes a <see cref="Host"/> library that is in use by the runtime
		/// </summary>
		/// <param name="assemblyPath">The path of the library to delete</param>
		void DeleteActiveLibrary(string assemblyPath);
	}
}