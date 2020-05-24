using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a BYOND installation
	/// </summary>
	public sealed class Byond
	{
		/// <summary>
		/// The <see cref="System.Version"/> of the <see cref="Byond"/> installation used for new compiles. Will be <see langword="null"/> if the user does not have permission to view it or there is no BYOND version installed. Only considers the <see cref="Version.Major"/> and <see cref="Version.Minor"/> numbers.
		/// </summary>
		public Version? Version { get; set; }

		/// <summary>
		/// The <see cref="Job"/> being used to install a new <see cref="Version"/>
		/// </summary>
		public Job? InstallJob { get; set; }
	}
}
