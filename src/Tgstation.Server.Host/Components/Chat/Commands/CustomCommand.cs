using System;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	sealed class CustomCommand : Command
	{
		ICustomCommandHandler handler;

		public void SetHandler(ICustomCommandHandler handler)
		{
			if (this.handler != null)
				throw new InvalidOperationException("SetHandler() already called!");
			this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
		}

		/// <inheritdoc />
		public override void Invoke(string arguments)
		{
			if (handler == null)
				throw new InvalidOperationException("SetHandler() has not been called!");
		}
	}
}
