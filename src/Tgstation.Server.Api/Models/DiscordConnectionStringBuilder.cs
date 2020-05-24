using System;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// <see cref="ChatConnectionStringBuilder"/> for <see cref="ChatProvider.Discord"/>
	/// </summary>
	public sealed class DiscordConnectionStringBuilder : ChatConnectionStringBuilder
	{
		/// <inheritdoc />
		public override bool Valid => !String.IsNullOrEmpty(BotToken);

		/// <summary>
		/// The Discord bot token
		/// </summary>
		/// <remarks>See https://discordapp.com/developers/docs/topics/oauth2#bots</remarks>
		public string? BotToken { get; set; }

		/// <summary>
		/// Construct a <see cref="DiscordConnectionStringBuilder"/>
		/// </summary>
		public DiscordConnectionStringBuilder() { }

		/// <summary>
		/// Construct a <see cref="DiscordConnectionStringBuilder"/> from a <paramref name="connectionString"/>
		/// </summary>
		/// <param name="connectionString">The connection string</param>
		public DiscordConnectionStringBuilder(string connectionString)
		{
			BotToken = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
		}

		/// <inheritdoc />
		public override string ToString() => BotToken ?? "(null)";
	}
}