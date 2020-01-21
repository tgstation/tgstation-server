using System.Reflection;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// For retrieving the <see cref="Assembly"/>'s location.
	/// </summary>
	interface IAssemblyInformationProvider
	{
		/// <summary>
		/// Gets the path to the executing assembly.
		/// </summary>
		string Path { get; }

		/// <summary>
		/// Gets the <see cref="AssemblyName"/>.
		/// </summary>
		AssemblyName Name { get; }
	}
}
