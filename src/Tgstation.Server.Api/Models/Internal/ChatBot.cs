using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Manage the server chat bots
	/// </summary>
	public class ChatBot
	{
		/// <summary>
		/// The settings id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The name of the connection
		/// </summary>
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// If the connection is enabled
		/// </summary>
		public bool? Enabled { get; set; }

		/// <summary>
		/// The <see cref="ChatProvider"/> used for the connection
		/// </summary>
		public ChatProvider? Provider { get; set; }

		/// <summary>
		/// The information used to connect to the <see cref="Provider"/>
		/// </summary>
		[Required]
		public string ConnectionString { get; set; }
	}
}
