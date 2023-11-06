using System;
using System.Threading;

using Microsoft.AspNetCore.SignalR.Client;

namespace Tgstation.Server.Client
{
	/// <summary>
	/// A <see cref="IRetryPolicy"/> that attempts to refresh a given <see cref="apiClient"/>'s token on the first disconnect.
	/// </summary>
	sealed class ApiClientTokenRefreshRetryPolicy : IRetryPolicy
	{
		/// <summary>
		/// The backing <see cref="ApiClient"/>.
		/// </summary>
		readonly ApiClient apiClient;

		/// <summary>
		/// The wrapped <see cref="IRetryPolicy"/>.
		/// </summary>
		readonly IRetryPolicy wrappedPolicy;

		/// <summary>
		/// Initializes a new instance of the <see cref="ApiClientTokenRefreshRetryPolicy"/> class.
		/// </summary>
		/// <param name="apiClient">The value of <see cref="apiClient"/>.</param>
		/// <param name="wrappedPolicy">The value of <see cref="wrappedPolicy"/>.</param>
		public ApiClientTokenRefreshRetryPolicy(ApiClient apiClient, IRetryPolicy wrappedPolicy)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			this.wrappedPolicy = wrappedPolicy ?? throw new ArgumentNullException(nameof(wrappedPolicy));
		}

		/// <inheritdoc />
		public TimeSpan? NextRetryDelay(RetryContext retryContext)
		{
			if (retryContext == null)
				throw new ArgumentNullException(nameof(retryContext));

			if (retryContext.PreviousRetryCount == 0)
				AttemptTokenRefresh();

			return wrappedPolicy.NextRetryDelay(retryContext);
		}

		/// <summary>
		/// Attempt to refresh the <see cref="apiClient"/>s token asynchronously.
		/// </summary>
		async void AttemptTokenRefresh()
		{
			try
			{
				await apiClient.RefreshToken(CancellationToken.None);
			}
			catch
			{
				// intentionally ignored
			}
		}
	}
}
