using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Base <see langword="class"/> for implementing <see cref="IGitRemoteFeatures"/>.
	/// </summary>
	abstract class GitRemoteFeaturesBase : IGitRemoteFeatures
	{
		/// <inheritdoc />
		public abstract string TestMergeRefSpecFormatter { get; }

		/// <inheritdoc />
		public abstract string TestMergeLocalBranchNameFormatter { get; }

		/// <inheritdoc />
		public abstract RemoteGitProvider? RemoteGitProvider { get; }

		/// <inheritdoc />
		public abstract string RemoteRepositoryOwner { get; }

		/// <inheritdoc />
		public abstract string RemoteRepositoryName { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitRemoteFeaturesBase"/>.
		/// </summary>
		protected ILogger<GitRemoteFeaturesBase> Logger { get; }

		/// <summary>
		/// Cache of created <see cref="Models.TestMerge"/>s.
		/// </summary>
		readonly Dictionary<TestMergeParameters, Models.TestMerge> cachedLookups;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitRemoteFeaturesBase"/> <see langword="class"/>.
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="remoteUrl">The remote repository <see cref="Uri"/>.</param>
		public GitRemoteFeaturesBase(ILogger<GitRemoteFeaturesBase> logger, Uri remoteUrl)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			if (remoteUrl == null)
				throw new ArgumentNullException(nameof(remoteUrl));

			cachedLookups = new Dictionary<TestMergeParameters, Models.TestMerge>();
		}

		/// <inheritdoc />
		public async Task<Models.TestMerge> GetTestMerge(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));
			if (repositorySettings == null)
				throw new ArgumentNullException(nameof(repositorySettings));

			Models.TestMerge result;
			lock (cachedLookups)
				if (cachedLookups.TryGetValue(parameters, out result))
					Logger.LogTrace("Using cache for test merge #{0}", parameters.Number);

			if (result == null)
			{
				Logger.LogTrace("Retrieving metadata for test merge #{0}...", parameters.Number);
				result = await GetTestMergeImpl(parameters, repositorySettings, cancellationToken).ConfigureAwait(false);
				lock (cachedLookups)
					if (!cachedLookups.TryAdd(parameters, result))
						Logger.LogError("Race condition on adding test merge #{0}!", parameters.Number);
			}

			return result;
		}

		/// <summary>
		/// Implementation of <see cref="GetTestMerge(TestMergeParameters, RepositorySettings, CancellationToken)"/>
		/// </summary>
		/// <param name="parameters">The <see cref="TestMergeParameters"/>.</param>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in the <see cref="Models.TestMerge"/> of the <paramref name="parameters"/>.</returns>
		protected abstract Task<Models.TestMerge> GetTestMergeImpl(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken);
	}
}
