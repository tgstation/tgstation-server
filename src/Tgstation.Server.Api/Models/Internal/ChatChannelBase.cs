using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Base chat channel class.
	/// </summary>
	public abstract class ChatChannelBase
	{
		/// <summary>
		/// If the <see cref="ChatChannel"/> is an admin channel.
		/// </summary>
		/// <example>false</example>
		[Required]
		public bool? IsAdminChannel { get; set; }

		/// <summary>
		/// If the <see cref="ChatChannel"/> is a watchdog channel.
		/// </summary>
		/// <example>false</example>
		[Required]
		public bool? IsWatchdogChannel { get; set; }

		/// <summary>
		/// If the <see cref="ChatChannel"/> is an updates channel.
		/// </summary>
		/// <example>false</example>
		[Required]
		public bool? IsUpdatesChannel { get; set; }

		/// <summary>
		/// If the <see cref="ChatChannel"/> received system messages.
		/// </summary>
		[Required]
		public bool? IsSystemChannel { get; set; }

		/// <summary>
		/// A custom tag users can define to group channels together.
		/// </summary>
		/// <example>system-channel-only</example>
		[ResponseOptions]
		[StringLength(Limits.MaximumStringLength)]
		public string? Tag { get; set; }
	}
}
