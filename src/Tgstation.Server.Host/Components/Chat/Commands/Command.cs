using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <inheritdoc />
	public abstract class Command : ICommand
	{
		/// <inheritdoc />
		public string Name { get; protected set; }

		/// <inheritdoc />
		public string HelpText { get; protected set; }

		/// <inheritdoc />
		public bool AdminOnly { get; protected set; }

		/// <inheritdoc />
		public abstract Task<string> Invoke(string arguments, CancellationToken cancellationToken);
	}
}
