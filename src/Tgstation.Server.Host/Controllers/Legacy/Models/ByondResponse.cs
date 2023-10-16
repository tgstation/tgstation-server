using System;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Controllers.Legacy.Models
{
	/// <summary>
	/// Represents an installed BYOND <see cref="Version"/>.
	/// </summary>
	public sealed class ByondResponse
	{
		/// <summary>
		/// The installed BYOND <see cref="global::System.Version"/>. BYOND itself only considers the <see cref="Version.Major"/> and <see cref="Version.Minor"/> numbers. This older API uses the <see cref="Version.Build"/> number to represent installed custom versions.
		/// </summary>
		[ResponseOptions]
		public Version Version { get; set; }
	}
}
