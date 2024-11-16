#if NETSTANDARD2_0_OR_GREATER
using System;

namespace Tgstation.Server.Common.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="Version"/> class.
	/// </summary>
	public static class VersionExtensions
	{
		/// <summary>
		/// Converts a given <paramref name="version"/> into one with only <see cref="Version.Major"/>, <see cref="Version.Minor"/>, and <see cref="Version.Build"/>.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to convert.</param>
		/// <returns>A semver <see cref="Version"/> base on <paramref name="version"/>.</returns>
		public static Version Semver(this Version version)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			return new Version(
				version.Major,
				version.Minor,
				version.Build == -1 ? 0 : version.Build);
		}
	}
}
#endif
