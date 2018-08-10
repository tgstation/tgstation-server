using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Represents a command from DD
	/// </summary>
	sealed class CommCommand
	{
		/// <summary>
		/// The raw JSON decond of the <see cref="CommCommand"/>
		/// </summary>
		public IReadOnlyDictionary<string, string> Parameters { get; set; }
	}
}