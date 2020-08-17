using System;
using System.Reflection;
using Tgstation.Server.Api;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class AssemblyInformationProvider : IAssemblyInformationProvider
	{
		/// <inheritdoc />
		public string VersionPrefix => "tgstation-server";

		/// <inheritdoc />
		public Version Version { get; }

		/// <inheritdoc />
		public AssemblyName AssemblyName { get; }

		/// <inheritdoc />
		public string Path { get; }

		/// <inheritdoc />
		public string VersionString { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblyInformationProvider"/> <see langword="class"/>.
		/// </summary>
		public AssemblyInformationProvider()
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			Path = assembly.Location;
			AssemblyName = assembly.GetName();
			Version = AssemblyName.Version.Semver();
			VersionString = String.Concat(VersionPrefix, "-v", Version);
		}
	}
}
