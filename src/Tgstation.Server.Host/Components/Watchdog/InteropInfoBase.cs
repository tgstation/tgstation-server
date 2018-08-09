using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Components.Watchdog
{
	/// <summary>
	/// Information used in for reattaching and interop
	/// </summary>
	public class InteropInfoBase
	{
		/// <summary>
		/// Path to the chat commands json file
		/// </summary>
		[Required]
		public string ChatCommandsJson { get; set; }

		/// <summary>
		/// Path to the chat channels json file
		/// </summary>
		[Required]
		public string ChatChannelsJson { get; set; }

		/// <summary>
		/// Path to the server commands json file
		/// </summary>
		[Required]
		public string ServerCommandsJson { get; set; }

		/// <summary>
		/// Construct an <see cref="InteropInfoBase"/>
		/// </summary>
		protected InteropInfoBase() { }

		/// <summary>
		/// Construct an <see cref="InteropInfoBase"/> from a <paramref name="copy"/>
		/// </summary>
		/// <param name="copy">An <see cref="InteropInfoBase"/> to copy</param>
		public InteropInfoBase(InteropInfoBase copy)
		{
			if (copy == null)
				throw new ArgumentNullException(nameof(copy));
			ChatChannelsJson = copy.ChatChannelsJson;
			ChatCommandsJson = copy.ChatCommandsJson;
			ServerCommandsJson = copy.ServerCommandsJson;
		}
	}
}
