using System;
using System.Threading;
using System.Threading.Tasks;

namespace Tgstation.Server.Host.Security
{
	/// <summary>
	/// For keeping a specific <see cref="ISystemIdentity"/> alive for a period of time
	/// </summary>
	sealed class IdentityCache : IDisposable
	{
		/// <summary>
		/// The <see cref="ISystemIdentity"/> the <see cref="IdentityCache"/> manages
		/// </summary>
		public ISystemIdentity SystemIdentity { get; }

		/// <summary>
		/// The <see cref="cancellationTokenSource"/> for the <see cref="IdentityCache"/>
		/// </summary>
		readonly CancellationTokenSource cancellationTokenSource;

		/// <summary>
		/// The <see cref="Task"/> to clean up <see cref="SystemIdentity"/>
		/// </summary>
		readonly Task task;

		/// <summary>
		/// Construct an <see cref="IdentityCache"/>
		/// </summary>
		/// <param name="systemIdentity">The value of <see cref="SystemIdentity"/></param>
		/// <param name="expiry">The <see cref="DateTimeOffset"/></param>
		/// <param name="onExpiry">An optional <see cref="Action"/> to take on expiry</param>
		public IdentityCache(ISystemIdentity systemIdentity, DateTimeOffset expiry, Action onExpiry)
		{
			SystemIdentity = systemIdentity ?? throw new ArgumentNullException(nameof(systemIdentity));

			cancellationTokenSource = new CancellationTokenSource();

		 	async Task DisposeOnExipiry(CancellationToken cancellationToken)
			{
				using (SystemIdentity)
					try
					{
						await Task.Delay(expiry - DateTimeOffset.Now, cancellationToken).ConfigureAwait(false);
					}
					finally
					{
						onExpiry?.Invoke();
					}
			}

			task = DisposeOnExipiry(cancellationTokenSource.Token);
		}

		/// <inheritdoc />
		public void Dispose()
		{
			cancellationTokenSource.Cancel();
			task.Wait();
			cancellationTokenSource.Dispose();
		}
	}
}
