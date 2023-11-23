using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Interop;
using Tgstation.Server.Host.System;

#nullable disable

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// <see cref="ICommand"/> to return the <see cref="IAssemblyInformationProvider.VersionString"/>.
	/// </summary>
	sealed class VersionCommand : ICommand
	{
		/// <inheritdoc />
		public string Name => "version";

		/// <inheritdoc />
		public string HelpText => "Displays the tgstation server version";

		/// <inheritdoc />
		public bool AdminOnly => false;

		/// <summary>
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="VersionCommand"/>.
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="VersionCommand"/> class.
		/// </summary>
		/// <param name="assemblyInformationProvider">The value of <see cref="assemblyInformationProvider"/>.</param>
		public VersionCommand(IAssemblyInformationProvider assemblyInformationProvider)
		{
			this.assemblyInformationProvider = assemblyInformationProvider ?? throw new ArgumentNullException(nameof(assemblyInformationProvider));
		}

		/// <inheritdoc />
		public ValueTask<MessageContent> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken) => ValueTask.FromResult(new MessageContent
		{
			Text = assemblyInformationProvider.VersionString,
		});
	}
}
