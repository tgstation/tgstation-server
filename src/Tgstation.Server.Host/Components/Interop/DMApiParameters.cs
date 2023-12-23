using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Common base for interop parameters.
	/// </summary>
	public abstract class DMApiParameters
	{
		/// <summary>
		/// Used to identify and authenticate the DreamDaemon instance.
		/// </summary>
		[Required]
		public string? AccessIdentifier { get; set; }
	}
}
