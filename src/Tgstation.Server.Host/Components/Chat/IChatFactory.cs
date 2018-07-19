namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// For creating <see cref="IChat"/>s
	/// </summary>
	interface IChatFactory
	{
		/// <summary>
		/// Create a <see cref="IChat"/>
		/// </summary>
		/// <returns>A new <see cref="IChat"/></returns>
		IChat CreateChat();
	}
}
