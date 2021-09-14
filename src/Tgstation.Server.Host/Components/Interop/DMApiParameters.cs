using System;

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
		public virtual string AccessIdentifier { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="DMApiParameters"/> class.
		/// </summary>
		[Obsolete("For helper initialization.", true)]
		protected DMApiParameters()
		{
			AccessIdentifier = default!;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DMApiParameters"/> class.
		/// </summary>
		/// <param name="accessIdentifier">The value of <see cref="AccessIdentifier"/>.</param>
		protected DMApiParameters(string accessIdentifier)
		{
			AccessIdentifier = accessIdentifier ?? throw new ArgumentNullException(nameof(accessIdentifier));
		}
	}
}
