using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Represents the state of a distributed swarm update.
	/// </summary>
	public class SwarmUpdateOperation
	{
		/// <summary>
		/// All of the <see cref="SwarmServer"/>s that are involved in the updates.
		/// </summary>
		public IReadOnlyList<SwarmServerInformation> InvolvedServers => initialInvolvedServers ?? throw new InvalidOperationException("This property can only be checked on controller SwarmUpdateOperations!");

		/// <summary>
		/// The <see cref="Version"/> being updated to.
		/// </summary>
		public Version TargetVersion { get; }

		/// <summary>
		/// The <see cref="Task{TResult}"/> that represents the final commit. If it results in <see langword="true"/> the update has been committed to and can no longer be aborted. If it results in <see langword="false"/>, it has been aborted.
		/// </summary>
		public Task<bool> CommitGate => commitTcs.Task;

		/// <summary>
		/// Backing field for <see cref="InvolvedServers"/>.
		/// </summary>
		readonly IReadOnlyList<SwarmServerInformation>? initialInvolvedServers;

		/// <summary>
		/// The backing <see cref="TaskCompletionSource{TResult}"/> for <see cref="CommitGate"/>.
		/// </summary>
		readonly TaskCompletionSource<bool> commitTcs;

		/// <summary>
		/// <see cref="HashSet{T}"/> of <see cref="SwarmServer.Identifier"/> that need to send a ready-commit to the controller before the commit can happen.
		/// </summary>
		readonly HashSet<string>? nodesThatNeedToBeReadyToCommit;

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmUpdateOperation"/> class.
		/// </summary>
		/// <param name="targetVersion">The value of <see cref="TargetVersion"/>.</param>
		/// <remarks>This is the variant for use by non-controller nodes.</remarks>
		public SwarmUpdateOperation(Version targetVersion)
		{
			TargetVersion = targetVersion ?? throw new ArgumentNullException(nameof(targetVersion));
			commitTcs = new TaskCompletionSource<bool>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SwarmUpdateOperation"/> class.
		/// </summary>
		/// <param name="targetVersion">The value of <see cref="TargetVersion"/>.</param>
		/// <param name="currentNodes">An <see cref="IEnumerable{T}"/> of the controller's current nodes as <see cref="SwarmServerInformation"/>s. Must have <see cref="SwarmServer.Address"/> and <see cref="SwarmServer.Identifier"/> set.</param>
		/// <remarks>This is the variant for use by the controller.</remarks>
		public SwarmUpdateOperation(Version targetVersion, IEnumerable<SwarmServerInformation> currentNodes)
			: this(targetVersion)
		{
			initialInvolvedServers = currentNodes?.ToList() ?? throw new ArgumentNullException(nameof(currentNodes));
			nodesThatNeedToBeReadyToCommit = initialInvolvedServers
				.Where(node => !node.Controller)
				.Select(node => node.Identifier!)
				.ToHashSet();
		}

		/// <summary>
		/// Attempt to abort the update operation.
		/// </summary>
		/// <returns>The <see cref="SwarmUpdateAbortResult"/>.</returns>
		public SwarmUpdateAbortResult Abort()
		{
			if (commitTcs.TrySetResult(false))
				return SwarmUpdateAbortResult.Aborted;

			return commitTcs.Task.Result
				? SwarmUpdateAbortResult.CantAbortCommitted
				: SwarmUpdateAbortResult.AlreadyAborted;
		}

		/// <summary>
		/// Attempt to commit the update.
		/// </summary>
		/// <returns><see langword="true"/> if the commit was successful, <see langword="false"/> if the commit already happened.</returns>
		public bool Commit()
		{
			if (nodesThatNeedToBeReadyToCommit != null && nodesThatNeedToBeReadyToCommit.Count != 0)
				throw new InvalidOperationException($"Cannot commit! There are still {nodesThatNeedToBeReadyToCommit.Count} nodes that need to be ready!");

			if (commitTcs.Task.IsCompleted && !commitTcs.Task.Result)
				return false;

			commitTcs.SetResult(true); // let the InvalidOperationException throw
			return true;
		}

		/// <summary>
		/// Marks a <see cref="SwarmServer"/> identified by <paramref name="nodeIdentifier"/> as ready to commit.
		/// </summary>
		/// <param name="nodeIdentifier">The <see cref="SwarmServer.Identifier"/> to mark as ready.</param>
		/// <returns><see langword="true"/> on success, <see langword="false"/> if the update is aborting.</returns>
		public bool MarkNodeReady(string nodeIdentifier)
		{
			ArgumentNullException.ThrowIfNull(nodeIdentifier);

			if (nodesThatNeedToBeReadyToCommit == null)
				throw new InvalidOperationException("A non-controller node tried to mark a node as ready!");

			lock (nodesThatNeedToBeReadyToCommit)
			{
				if (!nodesThatNeedToBeReadyToCommit.Remove(nodeIdentifier))
					return Abort() != SwarmUpdateAbortResult.Aborted;

				if (nodesThatNeedToBeReadyToCommit.Count == 0)
					commitTcs.TrySetResult(true);

				return true;
			}
		}
	}
}
