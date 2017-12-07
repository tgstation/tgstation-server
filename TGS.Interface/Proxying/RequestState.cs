namespace TGS.Interface.Proxying
{
	/// <summary>
	/// Denotes the state of the request
	/// </summary>
	public enum RequestState
	{
		/// <summary>
		/// An error occurred while processing the request, most likely it was badly formed
		/// </summary>
		Invalid,
		/// <summary>
		/// The provided credentials did not authenticate
		/// </summary>
		Unauthenticated,
		/// <summary>
		/// The provided user is not authorized to make the request
		/// </summary>
		Unauthorized,
		/// <summary>
		/// The request is in progress
		/// </summary>
		InProgress,
		/// <summary>
		/// The request has finished
		/// </summary>
		Finished,
		/// <summary>
		/// The <see cref="RequestInfo.RequestToken"/> field was incorrect
		/// </summary>
		BadToken,
	}
}
