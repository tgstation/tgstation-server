using System;
using System.Collections.Generic;

using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// Factory for <see cref="IProvider"/>s.
	/// </summary>
	interface IProviderFactory
	{
		/// <summary>
		/// Create a <see cref="IProvider"/>.
		/// </summary>
		/// <param name="settings">The <see cref="ChatBot"/> containing settings for the new provider.</param>
		/// <param name="commandNamesFactory">Gets the currently available chat command names.</param>
		/// <returns>A new <see cref="IProvider"/>.</returns>
		IProvider CreateProvider(ChatBot settings, Func<IReadOnlyList<string>> commandNamesFactory);
	}
}
