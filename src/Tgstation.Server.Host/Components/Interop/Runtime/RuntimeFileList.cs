using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Components.Interop.Runtime
{
	/// <summary>
	/// Information used in for reattaching and interop
	/// </summary>
	public class RuntimeFileList
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
		/// Construct an <see cref="RuntimeFileList"/>
		/// </summary>
		protected RuntimeFileList() { }

		/// <summary>
		/// Construct an <see cref="RuntimeFileList"/> from a <paramref name="copy"/>
		/// </summary>
		/// <param name="copy">An <see cref="RuntimeFileList"/> to copy</param>
		public RuntimeFileList(RuntimeFileList copy)
		{
			if (copy == null)
				throw new ArgumentNullException(nameof(copy));
			ChatChannelsJson = copy.ChatChannelsJson;
			ChatCommandsJson = copy.ChatCommandsJson;
		}
	}
}
