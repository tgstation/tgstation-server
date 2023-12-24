using System;
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
		public string AccessIdentifier { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DMApiParameters"/> class.
		/// </summary>
		/// <param name="accessIdentifier">The value of <see cref="AccessIdentifier"/>.</param>
		public DMApiParameters(string accessIdentifier)
		{
			AccessIdentifier = accessIdentifier ?? throw new ArgumentNullException(nameof(accessIdentifier));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DMApiParameters"/> class.
		/// </summary>
		/// <remarks>For use by EFCore only.</remarks>
		protected DMApiParameters()
		{
			AccessIdentifier = null!;
		}
	}
}
