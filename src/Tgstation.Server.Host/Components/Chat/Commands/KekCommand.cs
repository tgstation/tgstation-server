using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// kek
	/// </summary>
	sealed class KekCommand : BuiltinCommand
	{
		/// <inheritdoc />
		public override Task<string> Invoke(string arguments, User user, CancellationToken cancellationToken) => Task.FromResult("kek");
	}
}
