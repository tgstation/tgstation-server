using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a command from DD
	/// </summary>
	sealed class CommCommand
	{
		/// <summary>
		/// The dictionary of the <see cref="CommCommand"/>
		/// </summary>
		public IReadOnlyDictionary<string, object> Parameters { get; set; }

		/// <summary>
		/// The raw JSON of the <see cref="CommCommand"/>
		/// </summary>
		public string RawJson { get; set; }
	}
}