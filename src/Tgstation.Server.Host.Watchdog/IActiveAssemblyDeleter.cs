using System.Reflection;

namespace Tgstation.Server.Host.Watchdog
{
	/// <summary>
	/// For deleting <see cref="Assembly"/>s used by the program
	/// </summary>
	interface IActiveAssemblyDeleter
	{
		/// <summary>
		/// Deletes an <paramref name="assembly"/> that is in use by the runtime
		/// </summary>
		/// <param name="assembly">The <see cref="Assembly"/> to delete</param>
		void DeleteActiveAssembly(Assembly assembly);
	}
}