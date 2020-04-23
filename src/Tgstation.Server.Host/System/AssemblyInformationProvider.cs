using System;
using System.Reflection;
using Tgstation.Server.Host.Extensions;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class AssemblyInformationProvider : IAssemblyInformationProvider
	{
		/// <inheritdoc />
		public string VersionPrefix => "tgstation-server";

		/// <inheritdoc />
		public Version Version => Name.Version!;

		/// <inheritdoc />
		public AssemblyName Name { get; }

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
			Name = assembly.GetName();
			VersionString = String.Concat(VersionPrefix, '-', Version.Semver());
		}
	}
}
