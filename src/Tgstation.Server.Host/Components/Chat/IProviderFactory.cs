using Tgstation.Server.Api.Models.Internal;
using Tgstation.Server.Host.Components.Chat.Providers;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <summary>
	/// Factory for <see cref="IProvider"/>s
	/// </summary>
	interface IProviderFactory
	{
		/// <summary>
		/// Create a <see cref="IProvider"/>
		/// </summary>
		/// <param name="settings">The <see cref="ChatBot"/> containing settings for the new provider</param>
		/// <returns>A new <see cref="IProvider"/></returns>
		IProvider CreateProvider(ChatBot settings);
	}
}
