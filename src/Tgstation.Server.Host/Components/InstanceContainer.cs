using System;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Components
{
	/// <summary>
	/// Wrapper for managing <see cref="IInstance"/>s.
	/// </summary>
	sealed class InstanceContainer
	{
		/// <summary>
		/// The <see cref="IInstance"/>.
		/// </summary>
		public IInstance Instance { get; }

		/// <summary>
		/// A <see cref="Task"/> that completes when there are no <see cref="IInstanceReference"/>s active for the <see cref="Instance"/>.
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
		/// <see langword="lock"/> <see cref="object"/> for <see cref="referenceCount"/>.
		/// </summary>
		readonly object referenceCountLock;

		/// <summary>
		/// Backing <see cref="TaskCompletionSource"/> for <see cref="OnZeroReferences"/>.
		/// </summary>
		TaskCompletionSource onZeroReferencesTcs;

		/// <summary>
		/// Count of active <see cref="IInstanceReference"/>s.
		/// </summary>
		ulong referenceCount;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceContainer"/> class.
		/// </summary>
		/// <param name="instance">The value of <see cref="Instance"/>.</param>
		public InstanceContainer(IInstance instance)
		{
			Instance = instance ?? throw new ArgumentNullException(nameof(instance));

			referenceCountLock = new object();
			onZeroReferencesTcs = new TaskCompletionSource();
		}

		/// <summary>
		/// Create a new <see cref="IInstanceReference"/>.
		/// </summary>
		/// <returns>A new <see cref="IInstanceReference"/>.</returns>
		public IInstanceReference AddReference()
		{
			lock (referenceCountLock)
				try
				{
					return new InstanceWrapper(Instance, () =>
					{
						lock (referenceCountLock)
							if (--referenceCount == 0)
							{
								onZeroReferencesTcs.SetResult();
								onZeroReferencesTcs = new TaskCompletionSource();
							}
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
