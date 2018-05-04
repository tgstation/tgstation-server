using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Represents a control set for one or two DreamDaemon instances
	/// </summary>
	interface IInteropControl : IDisposable
	{
		/// <summary>
		/// When a server control message arrives
		/// </summary>
		event EventHandler<ServerControlEvent> OnServerControl;

		bool TwinServerMode { get; }

		bool SecondaryIsOther { get; }

		string PrimaryAccessToken { get; }

		string SecondaryAccessToken { get; }

		Task ActivateOtherServer(CancellationToken cancellationToken);

		Task ChangePort(ushort newPort, bool forPrimary, CancellationToken cancellationToken);
	}
}
