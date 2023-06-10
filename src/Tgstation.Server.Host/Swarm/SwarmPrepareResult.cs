namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Indicates the result of a swarm update prepare operation.
	/// </summary>
	public enum SwarmPrepareResult
	{
		/// <summary>
		/// Preparation failed.
		/// </summary>
		Failure,

		/// <summary>
		/// Preparation succeeded. The input <see cref="IO.ISeekableFileStreamProvider"/> must be kept available until after the swarm commit.
		/// </summary>
		SuccessHoldProviderUntilCommit,

		/// <summary>
		/// Preparation succeeded. The input <see cref="IO.ISeekableFileStreamProvider"/> is not longer required.
		/// </summary>
		SuccessProviderNotRequired,
	}
}
