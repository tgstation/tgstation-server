using System;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// <see cref="ICommand"/> to return the <see cref="IAssemblyInformationProvider.VersionString"/>
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
		/// The <see cref="IAssemblyInformationProvider"/> for the <see cref="VersionCommand"/>
		/// </summary>
		readonly IAssemblyInformationProvider assemblyInformationProvider;

		/// <summary>
		/// Construct a <see cref="VersionCommand"/>
		/// </summary>
		/// <param name="application">The value of <see cref="assemblyInformationProvider"/></param>
		public VersionCommand(IAssemblyInformationProvider application)
		{
			this.assemblyInformationProvider = application ?? throw new ArgumentNullException(nameof(application));
		}

		/// <inheritdoc />
		public Task<string> Invoke(string arguments, User user, CancellationToken cancellationToken) => Task.FromResult(assemblyInformationProvider.VersionString);
	}
}
