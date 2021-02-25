using System;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents an installed BYOND <see cref="Version"/>.
	/// </summary>
	public sealed class ByondResponse
	{
		/// <summary>
		/// The installed BYOND <see cref="System.Version"/>. If there is no BYOND version installed. Only considers the <see cref="Version.Major"/> and <see cref="Version.Minor"/> numbers.
		/// </summary>
		[ResponseOptions]
		public Version? Version { get; set; }
	}
}
