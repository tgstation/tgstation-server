namespace TGS.Interface
{
	public interface IRemoteConnectionProvider
	{
		/// <summary>
		/// Checks if the <see cref="IServerInterface"/> is setup for a remote connection
		/// </summary>
		bool IsRemoteConnection { get; }

		/// <summary>
		/// The <see cref="RemoteLoginInfo"/> for the <see cref="IRemoteConnectionProvider"/>. Is <see langword="null"/> for local connections
		/// </summary>
		RemoteLoginInfo LoginInfo { get; }
	}
}
