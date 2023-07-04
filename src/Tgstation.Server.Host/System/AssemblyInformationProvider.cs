using System;
using System.Net.Http.Headers;
using System.Reflection;

using Tgstation.Server.Common;
using Tgstation.Server.Common.Extensions;

namespace Tgstation.Server.Host.System
{
	/// <inheritdoc />
	sealed class AssemblyInformationProvider : IAssemblyInformationProvider
	{
		/// <inheritdoc />
		public string VersionPrefix => Constants.CanonicalPackageName;

		/// <inheritdoc />
		public Version Version { get; }

		/// <inheritdoc />
		public AssemblyName AssemblyName { get; }

		/// <inheritdoc />
		public string Path { get; }

		/// <inheritdoc />
		public string VersionString { get; }

		/// <inheritdoc />
		public ProductInfoHeaderValue ProductInfoHeaderValue => new (
			VersionPrefix,
			Version.ToString());

		/// <summary>
		/// Initializes a new instance of the <see cref="AssemblyInformationProvider"/> class.
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
