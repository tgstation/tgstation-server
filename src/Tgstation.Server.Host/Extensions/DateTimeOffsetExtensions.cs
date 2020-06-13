using System;
using System.Globalization;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="DateTimeOffset"/> <see langword="class"/>.
	/// </summary>
	static class DateTimeOffsetExtensions
	{
		/// <summary>
		/// Convert a given <paramref name="dateTimeOffset"/> into a <see cref="string"/> that can be used to stamp file creation times.
		/// </summary>
		/// <param name="dateTimeOffset">The <see cref="DateTimeOffset"/> to convert.</param>
		/// <returns><paramref name="dateTimeOffset"/> as a file stamp <see cref="string"/>.</returns>
		public static string ToFileStamp(this DateTimeOffset dateTimeOffset)
			=> dateTimeOffset.ToString("yyyyMMddhhmmss", CultureInfo.InvariantCulture);
	}
}
