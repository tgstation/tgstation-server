namespace TGS.Server
{
	/// <inheritdoc />
	public sealed class ServerFactory : IServerFactory
	{
		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="ServerFactory"/>
		/// </summary>
		readonly IIOManager IO;

		/// <summary>
		/// Construct a <see cref="ServerFactory"/>
		/// </summary>
		/// <param name="io">The value of <see cref="IO"/></param>
		public ServerFactory(IIOManager io)
		{
			IO = new IOManager();
		}

		/// <inheritdoc />
		public IServer CreateServer(ILogger logger)
		{
			return new Server(logger, ServerConfig.Load(IO), IO);
		}
	}
}
