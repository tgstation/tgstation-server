using System;
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
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? Name { get; set; }

		/// <summary>
		/// If the connection is enabled
		/// </summary>
		public bool? Enabled { get; set; }

		/// <summary>
		/// The time interval in minutes the chat bot attempts to reconnect if <see cref="Enabled"/> and disconnected. Must not be zero.
		/// </summary>
		[Required]
		[Range(1, UInt32.MaxValue)]
		public uint? ReconnectionInterval { get; set; }

		/// <summary>
		/// The maximum number of <see cref="ChatChannel"/>s the <see cref="ChatBot"/> may contain.
		/// </summary>
		[Required]
		public ushort? ChannelLimit { get; set; }

		/// <summary>
		/// The <see cref="ChatProvider"/> used for the connection
		/// </summary>
		[Required]
		[EnumDataType(typeof(ChatProvider))]
		public ChatProvider? Provider { get; set; }

		/// <summary>
		/// The information used to connect to the <see cref="Provider"/>
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumStringLength)]
		public string? ConnectionString { get; set; }

		/// <summary>
		/// Get the <see cref="ChatConnectionStringBuilder"/> which maps to the <see cref="ConnectionString"/>.
		/// </summary>
		/// <returns>A <see cref="ChatConnectionStringBuilder"/> for the <see cref="ChatBot"/>.</returns>
		public ChatConnectionStringBuilder? CreateConnectionStringBuilder()
		{
			if (ConnectionString == null)
				return null;
			return Provider switch
			{
				ChatProvider.Discord => new DiscordConnectionStringBuilder(ConnectionString),
				ChatProvider.Irc => new IrcConnectionStringBuilder(ConnectionString),
				_ => throw new InvalidOperationException("Invalid Provider!"),
			};
		}

		/// <summary>
		/// Set the <see cref="ChatConnectionStringBuilder"/> for the <see cref="ChatBot"/>. Also updates the <see cref="ConnectionString"/>.
		/// </summary>
		/// <param name="stringBuilder">The optional <see cref="ChatConnectionStringBuilder"/>.</param>
		public void SetConnectionStringBuilder(ChatConnectionStringBuilder stringBuilder)
		{
			ConnectionString = stringBuilder?.ToString() ?? throw new ArgumentNullException(nameof(stringBuilder));
		}
	}
}
