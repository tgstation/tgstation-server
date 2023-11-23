using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Interop;

#nullable disable

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// kek.
	/// </summary>
	sealed class KekCommand : ICommand
	{
		/// <summary>
		/// kek.
		/// </summary>
		const string Kek = "kek";

		/// <inheritdoc />
		public string Name => Kek;

		/// <inheritdoc />
		public string HelpText => Kek;

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <inheritdoc />
		public ValueTask<MessageContent> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken) => ValueTask.FromResult(new MessageContent
		{
			Text = Kek,
		});
	}
}
