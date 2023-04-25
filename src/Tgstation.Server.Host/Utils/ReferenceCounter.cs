using System;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Class used for counting references with <see cref="ReferenceCountingContainer{TWrapped, TReference}"/>.
	/// </summary>
	/// <typeparam name="TInstance">The reference <see langword="class"/>.</typeparam>
	abstract class ReferenceCounter<TInstance> : IDisposable
		where TInstance : class
	{
		/// <summary>
		/// The referenced <typeparamref name="TInstance"/>.
		/// </summary>
		protected TInstance Instance => actualInstance ?? throw UninitializedOrDisposedException();

		/// <summary>
		/// The <see langword="lock"/> object for <see cref="Initialize(TInstance, Action)"/> and <see cref="Dispose"/>.
		/// </summary>
		readonly object initDisposeLock;

		/// <summary>
		/// Backing field for <see cref="Instance"/>.
		/// </summary>
		TInstance actualInstance;

		/// <summary>
		/// The <see cref="Action"/> to take when <see cref="Dispose"/> is called.
		/// </summary>
		Action referenceCleanupAction;

		/// <summary>
		/// If the <see cref="ReferenceCounter{TInstance}"/> was initialized.
		/// </summary>
		bool initialized;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceCounter{TInstance}"/> class.
		/// </summary>
		protected ReferenceCounter()
		{
			initDisposeLock = new object();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			lock (initDisposeLock)
			{
				referenceCleanupAction?.Invoke();
				referenceCleanupAction = null;
				actualInstance = null;
			}
		}

		/// <summary>
		/// Initialize the <see cref="ReferenceCounter{TInstance}"/>.
		/// </summary>
		/// <param name="instance">The reference counted <typeparamref name="TInstance"/>.</param>
		/// <param name="referenceCleanupAction">The <see cref="Action"/> to take to clean up the reference.</param>
		public void Initialize(TInstance instance, Action referenceCleanupAction)
		{
			if (instance == null)
				throw new ArgumentNullException(nameof(instance));

			if (referenceCleanupAction == null)
				throw new ArgumentNullException(nameof(referenceCleanupAction));

			lock (initDisposeLock)
			{
				if (initialized)
					throw new InvalidOperationException($"{nameof(ReferenceCounter<TInstance>)} already initialized!");

				actualInstance = instance;
				this.referenceCleanupAction = referenceCleanupAction;
				initialized = true;
			}
		}

		/// <summary>
		/// Prevents the aquired reference from being dropped when <see cref="Dispose"/> is called.
		/// </summary>
		/// <remarks>This will prevent <see cref="ReferenceCountingContainer{TWrapped, TReference}.OnZeroReferences"/> from ever completing.</remarks>
		protected void DangerousDropReference()
		{
			referenceCleanupAction = null;
		}

		/// <summary>
		/// Throw the appropriate <see cref="InvalidOperationException"/> when the <see cref="ReferenceCounter{TInstance}"/> is uninitialized or disposed.
		/// </summary>
		/// <returns>A new <see cref="InvalidOperationException"/> to throw.</returns>
		InvalidOperationException UninitializedOrDisposedException()
		{
			if (initialized)
				return new ObjectDisposedException(nameof(ReferenceCounter<TInstance>));

			return new InvalidOperationException($"{nameof(ReferenceCounter<TInstance>)} not initialized!");
		}
	}
}
