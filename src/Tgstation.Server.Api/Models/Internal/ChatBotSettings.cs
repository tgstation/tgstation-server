using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Manage the server chat bots.
	/// </summary>
	public abstract class ChatBotSettings : NamedEntity
	{
		/// <summary>
		/// If the connection is enabled.
		/// </summary>
		public bool? Enabled { get; set; }

		/// <summary>
		/// The time interval in minutes the chat bot attempts to reconnect if <see cref="Enabled"/> and disconnected. Must not be zero.
		/// </summary>
		/// <example>60</example>
		[Required]
		[Range(1, UInt32.MaxValue)]
		public uint? ReconnectionInterval { get; set; }

		/// <summary>
		/// The maximum number of <see cref="ChatChannel"/>s the <see cref="ChatBotSettings"/> may contain.
		/// </summary>
		/// <example>5</example>
		[Required]
		public ushort? ChannelLimit { get; set; }

		/// <summary>
		/// The <see cref="ChatProvider"/> used for the connection.
		/// </summary>
		[Required]
		[RequestOptions(FieldPresence.Required, PutOnly = true)]
		[EnumDataType(typeof(ChatProvider))]
		public ChatProvider? Provider { get; set; }

		/// <summary>
		/// The information used to connect to the <see cref="Provider"/>.
		/// </summary>
		[Required]
		[RequestOptions(FieldPresence.Required, PutOnly = true)]
		[ResponseOptions]
		[StringLength(Limits.MaximumStringLength)]
		public string? ConnectionString { get; set; }

		/// <summary>
		/// Get the <see cref="ChatConnectionStringBuilder"/> which maps to the <see cref="ConnectionString"/>.
		/// </summary>
		/// <returns>A <see cref="ChatConnectionStringBuilder"/> for the <see cref="ChatBotSettings"/>.</returns>
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
		/// Set the <see cref="ChatConnectionStringBuilder"/> for the <see cref="ChatBotSettings"/>. Also updates the <see cref="ConnectionString"/>.
		/// </summary>
		/// <param name="stringBuilder">The optional <see cref="ChatConnectionStringBuilder"/>.</param>
		public void SetConnectionStringBuilder(ChatConnectionStringBuilder stringBuilder)
		{
			ConnectionString = stringBuilder?.ToString() ?? throw new ArgumentNullException(nameof(stringBuilder));
		}
	}
}
