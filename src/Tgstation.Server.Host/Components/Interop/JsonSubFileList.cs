using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Information used in for reattaching and interop
	/// </summary>
	public class JsonSubFileList
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
		/// Construct an <see cref="JsonSubFileList"/>
		/// </summary>
		protected JsonSubFileList() { }

		/// <summary>
		/// Construct an <see cref="JsonSubFileList"/> from a <paramref name="copy"/>
		/// </summary>
		/// <param name="copy">An <see cref="JsonSubFileList"/> to copy</param>
		public JsonSubFileList(JsonSubFileList copy)
		{
			if (copy == null)
				throw new ArgumentNullException(nameof(copy));
			ChatChannelsJson = copy.ChatChannelsJson;
			ChatCommandsJson = copy.ChatCommandsJson;
			ServerCommandsJson = copy.ServerCommandsJson;
		}
	}
}
