using System;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.Components.Interop;

namespace Tgstation.Server.Host.Components.Chat.Commands
{
	/// <summary>
	/// Represents a command made from DM code.
	/// </summary>
	public sealed class CustomCommand : ICommand
	{
		/// <inheritdoc />
		public string Name { get; }

		/// <inheritdoc />
		public string HelpText { get; }

		/// <inheritdoc />
		public bool AdminOnly { get; }

		/// <summary>
		/// The <see cref="ICustomCommandHandler"/> for the <see cref="CustomCommand"/>.
		/// </summary>
		ICustomCommandHandler? handler;

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomCommand"/> class.
		/// </summary>
		/// <param name="name">The value of <see cref="Name"/>.</param>
		/// <param name="helpText">The value of <see cref="HelpText"/>.</param>
		/// <param name="adminOnly">The value of <see cref="AdminOnly"/>.</param>
		public CustomCommand(string name, string helpText, bool adminOnly)
		{
			Name = name ?? throw new ArgumentNullException(nameof(name));
			HelpText = helpText ?? throw new ArgumentNullException(nameof(helpText));
			AdminOnly = adminOnly;
		}

		/// <summary>
		/// Set a new <paramref name="handler"/>.
		/// </summary>
		/// <param name="handler">The value of <see cref="handler"/>.</param>
		public void SetHandler(ICustomCommandHandler handler)
		{
			if (this.handler != null)
				throw new InvalidOperationException("SetHandler() already called!");
			this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
		}

		/// <inheritdoc />
		public ValueTask<MessageContent> Invoke(string arguments, ChatUser user, CancellationToken cancellationToken)
		{
			if (handler == null)
				throw new InvalidOperationException("SetHandler() has not been called!");
			return handler.HandleChatCommand(Name, arguments, user, cancellationToken);
		}
	}
}
