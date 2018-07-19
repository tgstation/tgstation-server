using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// <see cref="ICommand"/>s written in C#
	/// </summary>
	public abstract class BuiltinCommand : ICommand
	{
		/// <inheritdoc />
		public string Name { get; protected set; }

		/// <inheritdoc />
		public string HelpText { get; protected set; }

		/// <inheritdoc />
		public bool AdminOnly { get; protected set; }

		/// <inheritdoc />
		public abstract Task<string> Invoke(string arguments, User user, CancellationToken cancellationToken);
	}
}
