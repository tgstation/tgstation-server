using System;
using System.Linq;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// <see cref="ChatConnectionStringBuilder"/> for <see cref="ChatProvider.Discord"/>.
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
		/// If the tgstation-server logo is shown in deployment embeds.
		/// </summary>
		public bool DeploymentBranding { get; set; }

		/// <summary>
		/// The <see cref="DiscordDMOutputDisplayType"/>.
		/// </summary>
		public DiscordDMOutputDisplayType DMOutputDisplay { get; set; }

		/// <summary>
		/// Currently unused. Note its origin in based meme before repurposing.
		/// </summary>
		readonly bool unusedFlag;

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscordConnectionStringBuilder"/> class.
		/// </summary>
		public DiscordConnectionStringBuilder()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DiscordConnectionStringBuilder"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		public DiscordConnectionStringBuilder(string connectionString)
		{
			if (connectionString == null)
				throw new ArgumentNullException(nameof(connectionString));

			var splits = connectionString.Split(';');

			BotToken = splits.First();

			if (splits.Length < 2 || !Enum.TryParse<DiscordDMOutputDisplayType>(splits[1], out var dMOutputDisplayType))
				dMOutputDisplayType = DiscordDMOutputDisplayType.Always;
			DMOutputDisplay = dMOutputDisplayType;

			if (splits.Length > 2 && Int32.TryParse(splits[2], out Int32 basedMeme))
				unusedFlag = Convert.ToBoolean(basedMeme);

			if (splits.Length > 3 && Int32.TryParse(splits[3], out Int32 branding))
				DeploymentBranding = Convert.ToBoolean(branding);
			else
				DeploymentBranding = true; // previous default behaviour
		}

		/// <inheritdoc />
		public override string ToString() => $"{BotToken};{(int)DMOutputDisplay};{Convert.ToInt32(unusedFlag)};{Convert.ToInt32(DeploymentBranding)}";
	}
}
