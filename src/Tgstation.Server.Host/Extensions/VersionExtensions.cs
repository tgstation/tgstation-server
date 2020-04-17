using System;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="Version"/> <see langword="class"/>.
	/// </summary>
	static class VersionExtensions
	{
		/// <summary>
		/// Converts a given <paramref name="version"/> into a semver <see cref="string"/>.
		/// </summary>
		/// <param name="version">The <see cref="Version"/> to convert.</param>
		/// <returns>A semver <see cref="string"/> base on <paramref name="version"/>.</returns>
		public static string Semver(this Version version)
		{
			if (version == null)
				throw new ArgumentNullException(nameof(version));

			return $"{version.Major}.{version.Minor}.{version.Build}";
		}
	}
}
