using System;
using System.Threading;

namespace Tgstation.Server.Host.System
{
	/// <summary>
	/// Contains a <see cref="CancellationToken"/> that triggers when the operating system requests the program shuts down.
	/// </summary>
	sealed class ProgramShutdownTokenSource : IDisposable
	{
		/// <summary>
		/// Lock <see cref="object"/> for <see cref="cancellationTokenSource"/>.
		/// </summary>
		readonly object tokenSourceAccessLock;

		/// <summary>
		/// The <see cref="CancellationTokenSource"/> for the <see cref="ProgramShutdownTokenSource"/>.
		/// </summary>
		CancellationTokenSource? cancellationTokenSource;

		/// <summary>
		/// Gets the <see cref="CancellationToken"/>.
		/// </summary>
		public CancellationToken Token => cancellationTokenSource?.Token ?? default;

		/// <summary>
		/// Initializes a new instance of the <see cref="ProgramShutdownTokenSource"/> class.
		/// </summary>
		public ProgramShutdownTokenSource()
		{
			tokenSourceAccessLock = new object();
			cancellationTokenSource = new CancellationTokenSource();

			AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
			{
				lock (tokenSourceAccessLock)
					cancellationTokenSource?.Cancel();
			};

			Console.CancelKeyPress += (sender, args) =>
			{
				args.Cancel = true;
				lock (tokenSourceAccessLock)
					cancellationTokenSource?.Cancel();
			};
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (tokenSourceAccessLock)
			{
				cancellationTokenSource?.Dispose();
				cancellationTokenSource = null;
			}
		}
	}
}
