using System;
using System.Linq;
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
		/// The Discord bot token.
		/// </summary>
		/// <remarks>See https://discordapp.com/developers/docs/topics/oauth2#bots</remarks>
		public string? BotToken { get; set; }

		/// <summary>
		/// <see cref="bool"/> to enable based mode. Will auto reply with a youtube link to a video that says "based on the hardware that's installed in it" to anyone saying 'based on what?' case-insensitive.
		/// </summary>
		public bool BasedMeme { get; set; }

		/// <summary>
		/// The <see cref="DiscordDMOutputDisplayType"/>.
		/// </summary>
		public DiscordDMOutputDisplayType DMOutputDisplay { get; set; }

		/// <summary>
		/// Construct a <see cref="DiscordConnectionStringBuilder"/>
		/// </summary>
		public DiscordConnectionStringBuilder()
		{
			BasedMeme = true;
		}

		/// <summary>
		/// Construct a <see cref="DiscordConnectionStringBuilder"/> from a <paramref name="connectionString"/>
		/// </summary>
		/// <param name="connectionString">The connection string</param>
		public DiscordConnectionStringBuilder(string connectionString)
		{
			if(connectionString == null)
				throw new ArgumentNullException(nameof(connectionString));

			var splits = connectionString.Split(';');
			BotToken = splits.First();
			if (splits.Length < 2 || !Enum.TryParse<DiscordDMOutputDisplayType>(splits[1], out var dMOutputDisplayType))
				dMOutputDisplayType = DiscordDMOutputDisplayType.Always;
			DMOutputDisplay = dMOutputDisplayType;
			if (splits.Length > 2 && Int32.TryParse(splits[2], out Int32 basedMeme))
			{
				BasedMeme = Convert.ToBoolean(basedMeme);
			}
			else
			{
				BasedMeme = true; // Oranges said this needs to be true by default :pensive:
			}
		}

		/// <inheritdoc />
		public override string ToString() => $"{BotToken};{(int)DMOutputDisplay};{Convert.ToInt32(BasedMeme)}";
	}
}
