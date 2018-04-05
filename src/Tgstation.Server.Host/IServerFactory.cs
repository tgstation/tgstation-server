namespace Tgstation.Server.Host
{
	/// <summary>
	/// For creating <see cref="IServer"/>s
	/// </summary>
	public interface IServerFactory
	{
		/// <summary>
		/// Create a <see cref="IServer"/>
		/// </summary>
		/// <returns>A new <see cref="IServer"/></returns>
		IServer CreateServer();
	}
}
