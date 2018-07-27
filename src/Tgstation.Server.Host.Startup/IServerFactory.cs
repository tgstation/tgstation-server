namespace Tgstation.Server.Host.Startup
{
	/// <summary>
	/// For creating <see cref="IServer"/>s
	/// </summary>
	public interface IServerFactory
	{
        /// <summary>
        /// Create a <see cref="IServer"/>
        /// </summary>
        /// <param name="args">The arguments for the <see cref="IServer"/></param>
		/// <param name="updatePath">The directory in which to install server updates</param>
        /// <returns>A new <see cref="IServer"/></returns>
        IServer CreateServer(string[] args, string updatePath);
	}
}
