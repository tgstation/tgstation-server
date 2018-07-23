using System;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a BYOND installation
	/// </summary>
	[Model(RightsType.Byond, RequiresInstance = true)]
	public sealed class Byond
	{
		/// <summary>
		/// The <see cref="System.Version"/> of the <see cref="Byond"/> installation used for new compiles. Will be <see langword="null"/> if the user does not have permission to view it or there is no BYOND version installed. Only considers the <see cref="Version.Major"/> and <see cref="Version.Minor"/> numbers
		/// </summary>
		[Permissions(ReadRight = ByondRights.ReadInstalled, WriteRight = ByondRights.ChangeVersion)]
		public Version Version { get; set; }
	}
}
