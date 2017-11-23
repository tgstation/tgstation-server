namespace TGS.Server.Logging
{
	/// <summary>
	/// Used for writing logs to a provider
	/// </summary>
	public interface ILogger
	{
		/// <summary>
		/// Writes information to the log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		/// <param name="loggingID">The 0-99 ID of the log source</param>
		void WriteInfo(string message, EventID id, byte loggingID);

		/// <summary>
		/// Writes an error to the log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		/// <param name="loggingID">The 0-99 ID of the log source</param>
		void WriteError(string message, EventID id, byte loggingID);

		/// <summary>
		/// Writes a warning to the log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		/// <param name="loggingID">The 0-99 ID of the log source</param>
		void WriteWarning(string message, EventID id, byte loggingID);

		/// <summary>
		/// Writes an access event to the log
		/// </summary>
		/// <param name="username">The (un)authenticated Windows user's name</param>
		/// <param name="authSuccess"><see langword="true"/> if <paramref name="username"/> authenticated sucessfully, <see langword="false"/> otherwise</param>
		/// <param name="loggingID">The 0-99 ID of the log source</param>
		void WriteAccess(string username, bool authSuccess, byte loggingID);
	}
}
