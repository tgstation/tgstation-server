using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Configurable settings for <see cref="DreamMaker"/>
	/// </summary>
	public class DreamMakerSettings
	{
		/// <summary>
		/// The .dme file <see cref="DreamMakerSettings"/> tries to compile with without the extension
		/// </summary>
		public string ProjectName { get; set; }

		/// <summary>
		/// The port used during compilation to validate the DMAPI
		/// </summary>
		[Required]
		public ushort? ApiValidationPort { get; set; }
	}
}
