using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Components.Byond;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// For displaying the installed Byond version
	/// </summary>
	sealed class ByondCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "byond";

		/// <inheritdoc />
		public string HelpText => "Displays the installed Byond version";

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <summary>
		/// the <see cref="IByondManager"/> for the <see cref="ByondCommand"/>
		/// </summary>
		readonly IByondManager byondManager;

		/// <summary>
		/// Construct a <see cref="ByondCommand"/>
		/// </summary>
		/// <param name="byondManager">The value of <see cref="byondManager"/></param>
		public ByondCommand(IByondManager byondManager)
		{
			this.byondManager = byondManager ?? throw new ArgumentNullException(nameof(byondManager));
		}

		/// <inheritdoc />
		public Task<string> Invoke(string arguments, User user, CancellationToken cancellationToken) => Task.FromResult(byondManager.ActiveVersion == null ? "None!" : String.Format(CultureInfo.InvariantCulture, "{0}.{1}", byondManager.ActiveVersion.Major, byondManager.ActiveVersion.Minor));
	}
}
