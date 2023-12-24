using System;
using System.Threading;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Runs a given <see cref="disposeAction"/> on <see cref="Dispose"/>.
	/// </summary>
	class DisposeInvoker : IDisposable
	{
		/// <summary>
		/// If <see cref="Dispose"/> was called.
		/// </summary>
		public bool IsDisposed => disposeRan != 0;

		/// <summary>
		/// The <see cref="Action"/> to run on <see cref="Dispose"/>.
		/// </summary>
		readonly Action disposeAction;

		/// <summary>
		/// An <see cref="int"/> representation of a <see cref="bool"/> indicating if <see cref="Dispose"/> has ran.
		/// </summary>
		volatile int disposeRan;

		/// <summary>
		/// Initializes a new instance of the <see cref="DisposeInvoker"/> class.
		/// </summary>
		/// <param name="disposeAction">The value of <see cref="disposeAction"/>.</param>
		public DisposeInvoker(Action disposeAction)
		{
			this.disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (Interlocked.Exchange(ref disposeRan, 1) != 0)
				return;

			DisposeImpl();
		}

		/// <summary>
		/// Implementation of <see cref="Dispose"/> run after reentrancy check.
		/// </summary>
		protected virtual void DisposeImpl() => disposeAction();
	}
}
