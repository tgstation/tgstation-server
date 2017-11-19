namespace TGS.Server
{
	/// <summary>
	/// <see langword="interface"/> for creating a <see cref="IServer"/>
	/// </summary>
	public interface IServerFactory
	{
		/// <summary>
		/// Creates an <see cref="IServer"/>
		/// </summary>
		/// <param name="logger">The <see cref="ILogger"/> for the <see cref="IServer"/></param>
		/// <returns>A new <see cref="IServer"/></returns>
		IServer CreateServer(ILogger logger);
	}
}
