using System;
using System.Net.Http.Headers;
using System.Reflection;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// For retrieving the <see cref="Assembly"/>'s location.
	/// </summary>
	public interface IAssemblyInformationProvider
	{
		/// <summary>
		/// Gets the path to the executing assembly.
		/// </summary>
		string Path { get; }

		/// <summary>
		/// Gets the <see cref="global::System.Reflection.AssemblyName"/>.
		/// </summary>
		AssemblyName AssemblyName { get; }

		/// <summary>
		/// Prefix to <see cref="VersionString"/>.
		/// </summary>
		string VersionPrefix { get; }

		/// <summary>
		/// A more verbose version of <see cref="Version"/>.
		/// </summary>
		string VersionString { get; }

		/// <summary>
		/// The version of the assembly.
		/// </summary>
		Version Version { get; }

		/// <summary>
		/// The <see cref="ProductInfoHeaderValue"/> for the assembly.
		/// </summary>
		ProductInfoHeaderValue ProductInfoHeaderValue { get; }
	}
}
