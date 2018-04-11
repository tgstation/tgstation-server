namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	sealed class ChatChannel : Api.Models.ChatChannel
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="ChatSettings.Id"/>
		/// </summary>
		public long ChatSettingsId { get; set; }

		/// <summary>
		/// The <see cref="Models.ChatSettings"/>
		/// </summary>
		public ChatSettings ChatSettings { get; set; }
	}
}
