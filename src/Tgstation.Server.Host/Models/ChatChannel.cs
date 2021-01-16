namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class ChatChannel : Api.Models.ChatChannel, IApiTransformable<Api.Models.ChatChannel>
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/>
		/// </summary>
		public long ChatSettingsId { get; set; }

		/// <summary>
		/// The <see cref="ChatBot"/>.
		/// </summary>
		public ChatBot ChatSettings { get; set; }

		/// <inheritdoc />
		public Api.Models.ChatChannel ToApi() => new Api.Models.ChatChannel
		{
			DiscordChannelId = DiscordChannelId,
			IsAdminChannel = IsAdminChannel,
			IsWatchdogChannel = IsWatchdogChannel,
			IsUpdatesChannel = IsUpdatesChannel,
			IrcChannel = IrcChannel,
			Tag = Tag
		};
	}
}
