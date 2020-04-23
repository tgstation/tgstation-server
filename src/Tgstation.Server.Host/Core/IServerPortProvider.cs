namespace Tgstation.Server.Host.Core
{
	/// <summary>
	/// Provides access to the server's <see cref="HttpApiPort"/>.
	/// </summary>
	public interface IServerPortProvider
	{
		/// <summary>
		/// The port the server listens on.
		/// </summary>
		ushort HttpApiPort { get; }
	}
}
