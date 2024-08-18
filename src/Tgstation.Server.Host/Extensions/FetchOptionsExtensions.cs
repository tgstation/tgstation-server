using System;
using System.Threading;

using LibGit2Sharp;
using LibGit2Sharp.Handlers;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="FetchOptions"/>.
	/// </summary>
	static class FetchOptionsExtensions
	{
		/// <summary>
		/// Hydrate a given set of <paramref name="fetchOptions"/>.
		/// </summary>
		/// <param name="fetchOptions">The <see cref="FetchOptions"/> to hydrate.</param>
		/// <param name="logger">The <see cref="ILogger"/> for the operation.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/>.</param>
		/// <param name="credentialsHandler">The optional <see cref="CredentialsHandler"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>The hydrated <paramref name="fetchOptions"/>.</returns>
		public static FetchOptions Hydrate(
			this FetchOptions fetchOptions,
			ILogger logger,
			JobProgressReporter progressReporter,
			CredentialsHandler credentialsHandler,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(fetchOptions);
			ArgumentNullException.ThrowIfNull(logger);

			fetchOptions.OnProgress = _ => !cancellationToken.IsCancellationRequested;
			fetchOptions.OnTransferProgress = transferProgress =>
			{
				if (progressReporter != null)
				{
					var percentage = ((double)transferProgress.IndexedObjects + transferProgress.ReceivedObjects) / (transferProgress.TotalObjects * 2);
					progressReporter.ReportProgress(percentage);
				}

				return !cancellationToken.IsCancellationRequested;
			};
			fetchOptions.OnUpdateTips = (_, _, _) => !cancellationToken.IsCancellationRequested;
			fetchOptions.CredentialsProvider = credentialsHandler;
			fetchOptions.RepositoryOperationStarting = _ => !cancellationToken.IsCancellationRequested;
			fetchOptions.OnTransferProgress = TransferProgressHandler(
				logger,
				progressReporter,
				cancellationToken);

			return fetchOptions;
		}

		/// <summary>
		/// Generate a <see cref="LibGit2Sharp.Handlers.TransferProgressHandler"/> from a given <paramref name="progressReporter"/> and <paramref name="cancellationToken"/>.
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the operation.</param>
		/// <param name="progressReporter">The <see cref="JobProgressReporter"/> of the operation.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A new <see cref="LibGit2Sharp.Handlers.TransferProgressHandler"/> based on <paramref name="progressReporter"/>.</returns>
		static TransferProgressHandler TransferProgressHandler(ILogger logger, JobProgressReporter progressReporter, CancellationToken cancellationToken) => transferProgress =>
		{
			double? percentage;
			var totalObjectsToProcess = transferProgress.TotalObjects * 2;
			var processedObjects = transferProgress.IndexedObjects + transferProgress.ReceivedObjects;
			if (totalObjectsToProcess < processedObjects || totalObjectsToProcess == 0)
				percentage = null;
			else
			{
				percentage = (double)processedObjects / totalObjectsToProcess;
				if (percentage < 0)
					percentage = null;
			}

			if (percentage == null)
				logger.LogDebug(
					"Bad transfer progress values (Please tell Cyberboss)! Indexed: {indexed}, Received: {received}, Total: {total}",
					transferProgress.IndexedObjects,
					transferProgress.ReceivedObjects,
					transferProgress.TotalObjects);

			progressReporter?.ReportProgress(percentage);
			return !cancellationToken.IsCancellationRequested;
		};
	}
}
