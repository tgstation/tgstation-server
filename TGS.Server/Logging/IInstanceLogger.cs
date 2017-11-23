namespace TGS.Server.Logging
{
	/// <summary>
	/// Logging interface that does not require a logging ID
	/// </summary>
	interface IInstanceLogger
	{
		/// <summary>
		/// Writes information to the Windows event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		void WriteInfo(string message, EventID id);

		/// <summary>
		/// Writes a warning to the Windows event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		void WriteWarning(string message, EventID id);

		/// <summary>
		/// Writes an error to the Windows event log
		/// </summary>
		/// <param name="message">The log message</param>
		/// <param name="id">The <see cref="EventID"/> of the message</param>
		void WriteError(string message, EventID id);

		/// <summary>
		/// Writes an access event to the Windows event log
		/// </summary>
		/// <param name="username">The (un)authenticated Windows user's name</param>
		/// <param name="authSuccess"><see langword="true"/> if <paramref name="username"/> authenticated sucessfully, <see langword="false"/> otherwise</param>
		void WriteAccess(string username, bool authSuccess);
	}
}
