using System.Threading;
using System.Threading.Tasks;

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
		public Task<string> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken) => Task.FromResult(Kek);
	}
}
