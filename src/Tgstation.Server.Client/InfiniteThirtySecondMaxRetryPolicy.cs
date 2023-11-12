using System;

using Microsoft.AspNetCore.SignalR.Client;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// A <see cref="IRetryPolicy"/> that returns seconds in powers of 2, maxing out at 30s.
	/// </summary>
	sealed class InfiniteThirtySecondMaxRetryPolicy : IRetryPolicy
	{
		/// <inheritdoc />
		public TimeSpan? NextRetryDelay(RetryContext retryContext)
		{
			if (retryContext == null)
				throw new ArgumentNullException(nameof(retryContext));

			return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, retryContext.PreviousRetryCount), 30));
		}
	}
}
