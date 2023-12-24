using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Wrapper for managing some <typeparamref name="TWrapped"/>.
	/// </summary>
	/// <typeparam name="TWrapped">The type being wrapped.</typeparam>
	/// <typeparam name="TReference">The disposable reference type returned.</typeparam>
	sealed class ReferenceCountingContainer<TWrapped, TReference>
		where TWrapped : class
		where TReference : ReferenceCounter<TWrapped>, new()
	{
		/// <summary>
		/// The <typeparamref name="TWrapped"/>.
		/// </summary>
		public TWrapped Instance { get; }

		/// <summary>
		/// A <see cref="Task"/> that completes when there are no <typeparamref name="TReference"/>s active for the <see cref="Instance"/>.
		/// </summary>
		public Task OnZeroReferences
		{
			get
			{
				lock (referenceCountLock)
				{
					if (referenceCount == 0)
						return Task.CompletedTask;
					return onZeroReferencesTcs!.Task;
				}
			}
		}

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for <see cref="referenceCount"/>.
		/// </summary>
		readonly object referenceCountLock;

		/// <summary>
		/// Backing <see cref="TaskCompletionSource"/> for <see cref="OnZeroReferences"/>.
		/// </summary>
		TaskCompletionSource? onZeroReferencesTcs;

		/// <summary>
		/// Count of active <see cref="Instance"/>s.
		/// </summary>
		ulong referenceCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceCountingContainer{TWrapped, TReference}"/> class.
		/// </summary>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		public ReferenceCountingContainer(TWrapped instance)
		{
			Instance = instance ?? throw new ArgumentNullException(nameof(instance));

			referenceCountLock = new object();
		}

		/// <summary>
		/// Create a new <typeparamref name="TReference"/> to the <see cref="Instance"/>.
		/// </summary>
		/// <returns>A new <typeparamref name="TReference"/>.</returns>
		public TReference AddReference()
		{
			lock (referenceCountLock)
			{
				if (referenceCount++ == 0)
					onZeroReferencesTcs = new TaskCompletionSource();

				try
				{
					var reference = new TReference();
					reference.Initialize(Instance, () =>
					{
						lock (referenceCountLock)
							if (--referenceCount == 0)
								onZeroReferencesTcs!.SetResult();
					});
					return reference;
				}
				catch
				{
					--referenceCount;
					throw;
				}
			}
		}
	}
}
