using System;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// Represents a command made from DM code
	/// </summary>
	public sealed class CustomCommand : Command
	{
		/// <summary>
		/// The <see cref="ICustomCommandHandler"/> for the <see cref="CustomCommand"/>
		/// </summary>
		ICustomCommandHandler handler;

		/// <summary>
		/// Set a new <paramref name="handler"/>
		/// </summary>
		/// <param name="handler">The value of <see cref="handler"/></param>
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
