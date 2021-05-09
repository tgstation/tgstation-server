using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// Abstraction for <see cref="global::System.Console"/>.
	/// </summary>
	interface IConsole
	{
		/// <summary>
		/// If the <see cref="IConsole"/> is visible to the user.
		/// </summary>
		bool Available { get; }

		/// <summary>
		/// Gets a <see cref="CancellationToken"/> that triggers if Crtl+C or an equivalent is pressed.
		/// </summary>
		CancellationToken CancelKeyPress { get; }

		/// <summary>
		/// Write some <paramref name="text"/> to the <see cref="IConsole"/>.
		/// </summary>
		/// <param name="text">The <see cref="string"/> to write.</param>
		/// <param name="newLine">If there should be a new line after the <paramref name="text"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task WriteAsync(string text, bool newLine, CancellationToken cancellationToken);

		/// <summary>
		/// Wait for a key press on the <see cref="IConsole"/>.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operations.</param>
		/// <returns>A <see cref="Task"/> representing the running operation.</returns>
		Task PressAnyKeyAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Read a line from the <see cref="IConsole"/>.
		/// </summary>
		/// <param name="usePasswordChar">If the input should be retrieved using the '*' character.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="string"/> read by the <see cref="IConsole"/>.</returns>
		Task<string> ReadLineAsync(bool usePasswordChar, CancellationToken cancellationToken);
	}
}
