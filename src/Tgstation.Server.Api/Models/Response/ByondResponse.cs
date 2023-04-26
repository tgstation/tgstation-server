using System;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an installed BYOND <see cref="Version"/>.
	/// </summary>
	public sealed class ByondResponse
	{
		/// <summary>
		/// The installed BYOND <see cref="System.Version"/>. BYOND itself only considers the <see cref="Version.Major"/> and <see cref="Version.Minor"/> numbers. TGS uses the <see cref="Version.Build"/> number to represent installed custom versions.
		/// </summary>
		[ResponseOptions]
		public Version? Version { get; set; }
	}
}
