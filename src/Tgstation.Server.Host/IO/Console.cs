using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Host.System;

namespace Tgstation.Server.Host.IO
{
	/// <summary>
	/// Implementation of <see cref="IConsole"/>, uses <see cref="global::System.Console"/>.
	/// </summary>
	sealed class Console : IConsole, IDisposable
	{
		/// <inheritdoc />
		public string Title
		{
			get => platformIdentifier.IsWindows
				? global::System.Console.Title
				: null;
			set => global::System.Console.Title = value;
		}

		/// <inheritdoc />
		public bool Available => Environment.UserInteractive;

		/// <inheritdoc />
		public CancellationToken CancelKeyPress => cancelKeyCts.Token;

		/// <summary>
		/// The <see cref="IPlatformIdentifier"/> for the <see cref="Console"/>.
		/// </summary>
		readonly IPlatformIdentifier platformIdentifier;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for <see cref="CancelKeyPress"/>.
		/// </summary>
		readonly CancellationTokenSource cancelKeyCts;

		/// <summary>
		/// If the <see cref="Console"/> was disposed.
		/// </summary>
		bool disposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="Console"/> class.
		/// </summary>
		/// <param name="platformIdentifier">The value of <see cref="platformIdentifier"/>.</param>
		public Console(IPlatformIdentifier platformIdentifier)
		{
			this.platformIdentifier = platformIdentifier ?? throw new ArgumentNullException(nameof(platformIdentifier));

			cancelKeyCts = new CancellationTokenSource();
			global::System.Console.CancelKeyPress += (sender, e) =>
			{
				lock (cancelKeyCts)
				{
					if (!disposed)
						cancelKeyCts.Cancel();
				}
			};
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (cancelKeyCts)
			{
				cancelKeyCts.Dispose();
				disposed = true;
			}
		}

		/// <inheritdoc />
		public Task PressAnyKeyAsync(CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				CheckAvailable();
				global::System.Console.Read();
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <inheritdoc />
		public Task<string> ReadLineAsync(bool usePasswordChar, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				// TODO: Make this better: https://stackoverflow.com/questions/9479573/how-to-interrupt-console-readline
				CheckAvailable();
				if (!usePasswordChar)
					return global::System.Console.ReadLine();

				var passwordBuilder = new StringBuilder();
				do
				{
					var keyDescription = global::System.Console.ReadKey(true);
					if (keyDescription.Key == ConsoleKey.Enter)
						break;
					else if (keyDescription.Key == ConsoleKey.Backspace)
					{
						if (passwordBuilder.Length > 0)
						{
							--passwordBuilder.Length;
							global::System.Console.Write("\b \b");
						}
					}
					else if (keyDescription.KeyChar != '\u0000')
					{
						// KeyChar == '\u0000' if the key pressed does not correspond to a printable character, e.g. F1, Pause-Break, etc
						passwordBuilder.Append(keyDescription.KeyChar);
						global::System.Console.Write('*');
					}
				}
				while (!cancellationToken.IsCancellationRequested);

				cancellationToken.ThrowIfCancellationRequested();
				global::System.Console.WriteLine();
				return passwordBuilder.ToString();
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current)
			.WaitAsync(cancellationToken);

		/// <inheritdoc />
		public Task WriteAsync(string text, bool newLine, CancellationToken cancellationToken) => Task.Factory.StartNew(
			() =>
			{
				CheckAvailable();
				if (text == null)
				{
					if (!newLine)
						throw new InvalidOperationException("Cannot write null text without a new line!");
					global::System.Console.WriteLine();
				}
				else if (newLine)
					global::System.Console.WriteLine(text);
				else
					global::System.Console.Write(text);
			},
			cancellationToken,
			DefaultIOManager.BlockingTaskCreationOptions,
			TaskScheduler.Current);

		/// <summary>
		/// Assert that the <see cref="Console"/> is available, throwing an <see cref="InvalidOperationException"/> otherwise.
		/// </summary>
		void CheckAvailable()
		{
			if (!Available)
				throw new InvalidOperationException("Console unavailable");
		}
	}
}
