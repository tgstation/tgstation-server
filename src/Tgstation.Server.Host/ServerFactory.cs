namespace Tgstation.Server.Host
{
	/// <inheritdoc />
	public sealed class ServerFactory : IServerFactory
	{
		/// <inheritdoc />
		public IServer CreateServer() => new Server();
	}
}
