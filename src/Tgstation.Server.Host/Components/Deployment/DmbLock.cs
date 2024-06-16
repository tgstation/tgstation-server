using System;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Host.Models;

namespace Tgstation.Server.Host.Components.Deployment
{
	/// <summary>
	/// Represents a lock on a given <see cref="IDmbProvider"/>.
	/// </summary>
	sealed class DmbLock : IDmbProvider
	{
		/// <inheritdoc />
		public string DmbName => baseProvider.DmbName;

		/// <inheritdoc />
		public string Directory => baseProvider.Directory;

		/// <inheritdoc />
		public CompileJob CompileJob => baseProvider.CompileJob;

		/// <inheritdoc />
		public EngineVersion EngineVersion => baseProvider.EngineVersion;

		/// <summary>
		/// Unique ID of the lock.
		/// </summary>
		public Guid LockID { get; }

		/// <summary>
		/// The <see cref="DateTimeOffset"/> of when the lock was acquired.
		/// </summary>
		public DateTimeOffset LockTime { get; }

		/// <summary>
		/// A description of the <see cref="DmbLock"/>'s purpose.
		/// </summary>
		public string Descriptor { get; }

		/// <summary>
		/// If <see cref="KeepAlive"/> was called on the <see cref="DmbLock"/>.
		/// </summary>
		public bool KeptAlive { get; private set; }

		/// <summary>
		/// The <see cref="IDmbProvider"/> being wrapped.
		/// </summary>
		readonly IDmbProvider baseProvider;

		/// <summary>
		/// A <see cref="Func{TResult}"/> to use as the implementation of <see cref="DisposeAsync"/>.
		/// </summary>
		readonly Func<ValueTask> disposeAction;

		/// <summary>
		/// Initializes a new instance of the <see cref="DmbLock"/> class.
		/// </summary>
		/// <param name="disposeAction">The value of <see cref="disposeAction"/>.</param>
		/// <param name="baseProvider">The value of <see cref="baseProvider"/>.</param>
		/// <param name="descriptor">The value of <see cref="Descriptor"/>.</param>
		public DmbLock(Func<ValueTask> disposeAction, IDmbProvider baseProvider, string descriptor)
		{
			this.disposeAction = disposeAction ?? throw new ArgumentNullException(nameof(disposeAction));
			this.baseProvider = baseProvider ?? throw new ArgumentNullException(nameof(baseProvider));
			Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));

			LockID = Guid.NewGuid();
			LockTime = DateTimeOffset.UtcNow;
		}

		/// <inheritdoc />
		public ValueTask DisposeAsync() => disposeAction();

		/// <inheritdoc />
		public void KeepAlive()
		{
			KeptAlive = true;
			baseProvider.KeepAlive();
		}
	}
}
