namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// For deleting <see cref="System.Reflection.Assembly"/>s used by the program
	/// </summary>
	interface IActiveAssemblyDeleter
	{
		/// <summary>
		/// Deletes an <see cref="System.Reflection.Assembly"/> that is in use by the runtime
		/// </summary>
		/// <param name="assemblyPath">The <see cref="System.Reflection.Assembly.Location"/> of the <see cref="System.Reflection.Assembly"/> to delete</param>
		void DeleteActiveAssembly(string assemblyPath);
	}
}