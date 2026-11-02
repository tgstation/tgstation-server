using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Base <see langword="class"/> for implementing <see cref="IGitRemoteFeatures"/>.
	/// </summary>
	abstract class GitRemoteFeaturesBase : IGitRemoteFeatures
	{
		/// <inheritdoc />
		public abstract string GetTestMergeRefSpec(TestMergeParameters parameters);

		/// <inheritdoc />
		public abstract string GetTestMergeLocalBranchName(TestMergeParameters parameters);

		/// <inheritdoc />
		public abstract RemoteGitProvider? RemoteGitProvider { get; }

		/// <inheritdoc />
		public string RemoteRepositoryOwner { get; }

		/// <inheritdoc />
		public string RemoteRepositoryName { get; }

		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="GitRemoteFeaturesBase"/>.
		/// </summary>
		protected ILogger<GitRemoteFeaturesBase> Logger { get; }

		/// <summary>
		/// The remote repository <see cref="Uri"/>.
		/// </summary>
		readonly Uri remoteUrl;

		/// <summary>
		/// Cache of created <see cref="Models.TestMerge"/>s.
		/// </summary>
		readonly Dictionary<TestMergeParameters, Models.TestMerge> cachedLookups;

		/// <summary>
		/// Initializes a new instance of the <see cref="GitRemoteFeaturesBase"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="remoteUrl">The remote repository <see cref="Uri"/>.</param>
		public GitRemoteFeaturesBase(ILogger<GitRemoteFeaturesBase> logger, Uri remoteUrl)
		{
			Logger = logger ?? throw new ArgumentNullException(nameof(logger));
			this.remoteUrl = remoteUrl ?? throw new ArgumentNullException(nameof(remoteUrl));

			RemoteRepositoryOwner = remoteUrl.Segments[1].TrimEnd('/');
			RemoteRepositoryName = remoteUrl.Segments[2].TrimEnd('/');
			if (RemoteRepositoryName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
				RemoteRepositoryName = RemoteRepositoryName[0..^4];

			cachedLookups = new Dictionary<TestMergeParameters, Models.TestMerge>();
		}

		/// <inheritdoc />
		public async ValueTask<Models.TestMerge> GetTestMerge(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken)
		{
			ArgumentNullException.ThrowIfNull(parameters);
			ArgumentNullException.ThrowIfNull(repositorySettings);

			Models.TestMerge? result;
			lock (cachedLookups)
				if (cachedLookups.TryGetValue(parameters, out result))
					Logger.LogTrace("Using cache for test merge #{testMergeNumber}", parameters.Number);

			if (result == null)
			{
				Logger.LogTrace("Retrieving metadata for test merge #{testMergeNumber}...", parameters.Number);
				result = await GetTestMergeImpl(parameters, repositorySettings, cancellationToken);
				lock (cachedLookups)
					if (!cachedLookups.TryAdd(parameters, result))
						Logger.LogError("Race condition on adding test merge #{testMergeNumber}!", parameters.Number);
			}

			return result;
		}

		/// <inheritdoc />
		public abstract ValueTask<string?> TransformRepositoryPassword(string? rawPassword, CancellationToken cancellationToken);

		/// <inheritdoc />
		public virtual Uri GetRemoteUrl(string owner, string name)
		{
			var builder = new UriBuilder(remoteUrl);
			var path = $"/{owner}/{name}";
			if (builder.Path.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
				path += ".git";

			builder.Path = path;
			return builder.Uri;
		}

		/// <inheritdoc />
		public (string Owner, string Name) GetRepositoryOwnerAndName(TestMergeParameters parameters)
		{
			ArgumentNullException.ThrowIfNull(parameters);
			if (parameters.SourceRepository == null)
				return (RemoteRepositoryOwner, RemoteRepositoryName);

			var parts = parameters.SourceRepository.Split('/', StringSplitOptions.RemoveEmptyEntries);
			return parts.Length == 2 ? (parts[0], parts[1]) : (RemoteRepositoryOwner, RemoteRepositoryName);
		}

		/// <summary>
		/// Implementation of <see cref="GetTestMerge(TestMergeParameters, RepositorySettings, CancellationToken)"/>.
		/// </summary>
		/// <param name="parameters">The <see cref="TestMergeParameters"/>.</param>
		/// <param name="repositorySettings">The <see cref="RepositorySettings"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in the <see cref="Models.TestMerge"/> of the <paramref name="parameters"/>.</returns>
		protected abstract ValueTask<Models.TestMerge> GetTestMergeImpl(
			TestMergeParameters parameters,
			RepositorySettings repositorySettings,
			CancellationToken cancellationToken);
	}
}
