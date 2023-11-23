using System.Collections.Generic;

using LibGit2Sharp;

#nullable disable

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Represents the result of a repository test merge attempt.
	/// </summary>
	public sealed class TestMergeResult
	{
		/// <summary>
		/// The resulting <see cref="MergeStatus"/>.
		/// </summary>
		public MergeStatus Status { get; init; }

		/// <summary>
		/// List of conflicting file paths relative to the repository root. Only present if <see cref="Status"/> is <see cref="MergeStatus.Conflicts"/>.
		/// </summary>
		public IReadOnlyList<string> ConflictingFiles { get; init; }
	}
}
