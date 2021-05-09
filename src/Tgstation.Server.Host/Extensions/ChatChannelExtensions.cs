using System;
using System.Collections.Generic;
using System.Linq;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="ChatChannel"/> class.
	/// </summary>
	static class ChatChannelExtensions
	{
		/// <summary>
		/// Gets the IRC channel name from a given <paramref name="chatChannel"/>.
		/// </summary>
		/// <param name="chatChannel">The <see cref="ChatChannel"/> to retrieve information from.</param>
		/// <returns>The IRC channel name stored in the <paramref name="chatChannel"/>.</returns>
		public static string GetIrcChannelName(this ChatChannel chatChannel) => GetIrcChannelSplits(chatChannel).First();

		/// <summary>
		/// Gets the IRC channel key from a given <paramref name="chatChannel"/>.
		/// </summary>
		/// <param name="chatChannel">The <see cref="ChatChannel"/> to retrieve information from.</param>
		/// <returns>The IRC channel key stored in the <paramref name="chatChannel"/> if it exists, <see langword="null"/> otherwise.</returns>
		public static string GetIrcChannelKey(this ChatChannel chatChannel)
		{
			var splits = GetIrcChannelSplits(chatChannel);
			if (splits.Count < 2)
				return null;
			return splits.Last();
		}

		/// <summary>
		/// Split a given <paramref name="chatChannel"/>'s <see cref="ChatChannel.IrcChannel"/>.
		/// </summary>
		/// <param name="chatChannel">The <see cref="ChatChannel"/> to work with.</param>
		/// <returns>A <see cref="IReadOnlyCollection{T}"/> of the <paramref name="chatChannel"/>'s <see cref="ChatChannel.IrcChannel"/> <see cref="string"/> separated by the ':' <see cref="char"/>.</returns>
		static IReadOnlyCollection<string> GetIrcChannelSplits(ChatChannel chatChannel)
		{
			if (chatChannel == null)
				throw new ArgumentNullException(nameof(chatChannel));

			if (chatChannel.IrcChannel == null)
				throw new ArgumentException("IrcChannel must be set!", nameof(chatChannel));

			return chatChannel.IrcChannel.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
