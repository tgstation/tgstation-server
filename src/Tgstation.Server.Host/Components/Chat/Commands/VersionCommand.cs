using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// <see cref="ICommand"/> to return the <see cref="IApplication.VersionString"/>
	/// </summary>
	sealed class VersionCommand : BuiltinCommand
	{
		/// <summary>
		/// The <see cref="IApplication"/> for the <see cref="VersionCommand"/>
		/// </summary>
		readonly IApplication application;

		/// <summary>
		/// Construct a <see cref="VersionCommand"/>
		/// </summary>
		/// <param name="application"></param>
		public VersionCommand(IApplication application)
		{
			this.application = application ?? throw new ArgumentNullException(nameof(application));
		}

		/// <inheritdoc />
		public override Task<string> Invoke(string arguments, User user, CancellationToken cancellationToken) => Task.FromResult(application.VersionString);
	}
}
