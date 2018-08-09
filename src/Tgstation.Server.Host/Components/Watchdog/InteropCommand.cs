using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Represents a command from DD
	/// </summary>
	sealed class InteropCommand
	{
		/// <summary>
		/// The raw JSON decond of the <see cref="InteropCommand"/>
		/// </summary>
		public IReadOnlyDictionary<string, string> Parameters { get; set; }
	}
}