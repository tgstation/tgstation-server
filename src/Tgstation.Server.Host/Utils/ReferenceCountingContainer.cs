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
		where TReference : IDisposable
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
					return onZeroReferencesTcs.Task;
				}
			}
		}

		/// <summary>
		/// The factory <see cref="Func{T, TResult}"/> for generating <typeparamref name="TReference"/>s to the <see cref="Instance"/>.
		/// </summary>
		readonly Func<TWrapped, Action, TReference> referenceFactory;

		/// <summary>
		/// <see langword="lock"/> <see cref="object"/> for <see cref="referenceCount"/>.
		/// </summary>
		readonly object referenceCountLock;

		/// <summary>
		/// Backing <see cref="TaskCompletionSource"/> for <see cref="OnZeroReferences"/>.
		/// </summary>
		TaskCompletionSource onZeroReferencesTcs;

		/// <summary>
		/// Count of active <see cref="Instance"/>s.
		/// </summary>
		ulong referenceCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="ReferenceCountingContainer{TWrapped, TReference}"/> class.
		/// </summary>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		/// <param name="referenceFactory">The value of <see cref="referenceFactory"/>.</param>
		public ReferenceCountingContainer(TWrapped instance, Func<TWrapped, Action, TReference> referenceFactory)
		{
			Instance = instance ?? throw new ArgumentNullException(nameof(instance));
			this.referenceFactory = referenceFactory ?? throw new ArgumentNullException(nameof(referenceFactory));

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
					return referenceFactory(Instance, () =>
					{
						lock (referenceCountLock)
							if (--referenceCount == 0)
								onZeroReferencesTcs.SetResult();
					});
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
