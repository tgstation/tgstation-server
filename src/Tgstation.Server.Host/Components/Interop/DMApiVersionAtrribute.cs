using System;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Attribute for bringing in the <see cref="DMApiConstants.Version"/> from MSBuild.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	sealed class DMApiVersionAtrribute : Attribute
	{
		/// <summary>
		/// The <see cref="Version"/> string of the DMAPI version built.
		/// </summary>
		public string RawDMApiVersion { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DMApiVersionAtrribute"/> <see langword="class"/>.
		/// </summary>
		/// <param name="rawDMApiVersion">The value of <see cref="RawDMApiVersion"/>.</param>
		public DMApiVersionAtrribute(string rawDMApiVersion)
		{
			RawDMApiVersion = rawDMApiVersion ?? throw new ArgumentNullException(nameof(rawDMApiVersion));
		}
	}
}
