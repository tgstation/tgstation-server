using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Octokit;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Service for interacting with GitHub.
	/// </summary>
	public interface IGitHubService
	{
		/// <summary>
		/// Gets the <see cref="Uri"/> of the repository designated as the updates repository.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Uri"/> of the designated updates repository.</returns>
		Task<Uri> GetUpdatesRepositoryUrl(CancellationToken cancellationToken);

		/// <summary>
		/// Get all valid TGS <see cref="Release"/>s from the configured update source.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a <see cref="Dictionary{TKey, TValue}"/> of TGS <see cref="Release"/>s keyed by their <see cref="Version"/>.</returns>
		/// <remarks>GitHub has been known to return incomplete results from the API with this call.</remarks>
		Task<Dictionary<Version, Release>> GetTgsReleases(CancellationToken cancellationToken);
	}
}
