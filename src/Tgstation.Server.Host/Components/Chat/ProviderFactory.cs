using System;
using System.Globalization;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Components.Chat.Providers;

namespace Tgstation.Server.Host.Components.Chat
{
	/// <inheritdoc />
	sealed class ProviderFactory : IProviderFactory
	{
		/// <inheritdoc />
		public IProvider CreateProvider(Api.Models.Internal.ChatSettings settings)
		{
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			switch (settings.Provider)
			{
				case ChatProvider.Irc:
					throw new NotImplementedException();
				case ChatProvider.Discord:
					throw new NotImplementedException();
				default:
					throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Invalid ChatProvider: {0}", settings.Provider));
			}
		}
	}
}
