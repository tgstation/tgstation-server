using System;

using Microsoft.EntityFrameworkCore;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class ChatChannel : Api.Models.ChatChannel, IApiTransformable<Api.Models.ChatChannel>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		public long ChatSettingsId { get; set; }

		/// <summary>
		/// The <see cref="ChatBot"/>.
		/// </summary>
		[BackingField(nameof(chatSettings))]
		public ChatBot ChatSettings
		{
			get => chatSettings ?? throw new InvalidOperationException("ChatSettings not set!");
			set => chatSettings = value;
		}

		/// <summary>
		/// Backing field for <see cref="ChatSettings"/>.
		/// </summary>
		ChatBot? chatSettings;

		/// <inheritdoc />
		public Api.Models.ChatChannel ToApi() => new ()
		{
			DiscordChannelId = DiscordChannelId,
			IsAdminChannel = IsAdminChannel,
			IsWatchdogChannel = IsWatchdogChannel,
			IsUpdatesChannel = IsUpdatesChannel,
			IrcChannel = IrcChannel,
			Tag = Tag,
		};
	}
}
