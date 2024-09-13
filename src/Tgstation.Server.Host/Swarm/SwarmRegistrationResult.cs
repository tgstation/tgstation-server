namespace Tgstation.Server.Host.Swarm
{
	/// <summary>
	/// Result of attempting to register with a swarm controller.
	/// </summary>
	public enum SwarmRegistrationResult
	{
		/// <summary>
		/// The registration succeeded.
		/// </summary>
		Success,

		/// <summary>
		/// The swarm private keys didn't match.
		/// </summary>
		Unauthorized,

		/// <summary>
		/// The swarm controller is running a different server version.
		/// </summary>
		VersionMismatch,

		/// <summary>
		/// A communication error occurred.
		/// </summary>
		CommunicationFailure,

		/// <summary>
		/// Response could not be deserialized.
		/// </summary>
		PayloadFailure,
	}
}
