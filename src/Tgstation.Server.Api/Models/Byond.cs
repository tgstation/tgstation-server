using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a BYOND installation
	/// </summary>
	public sealed class Byond
	{
		/// <summary>
		/// The <see cref="ByondStatus"/> for the <see cref="Byond"/> installation
		/// </summary>
		public ByondStatus ByondStatus { get; set; }

		/// <summary>
		/// The <see cref="System.Version"/> of the <see cref="Byond"/> installation. Will be <see langword="null"/> if the user does not have permission to view it or there is no BYOND version installed. Only considers the <see cref="Version.Major"/> and <see cref="Version.Minor"/> numbers
		/// </summary>
		public Version Version { get; set; }

		/// <summary>
		/// The <see cref="System.Version"/> of the <see cref="Byond"/> that's staged for installation
		/// </summary>
		public Version StagedVersion { get; set; }
	}
}
