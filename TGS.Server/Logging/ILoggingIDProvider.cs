namespace TGS.Server.Logging
{
	/// <summary>
	/// Gets logging IDs to be used with <see cref="ILogger"/>
	/// </summary>
	interface ILoggingIDProvider
	{
		/// <summary>
		/// Gets a logging ID
		/// </summary>
		/// <returns>A logging ID</returns>
		byte Get();

		/// <summary>
		/// Releases a logging ID
		/// </summary>
		/// <param name="loggingID">The logging ID to release</param>
		void Release(byte loggingID);
	}
}
