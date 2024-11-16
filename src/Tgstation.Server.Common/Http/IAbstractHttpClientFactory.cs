#if NETSTANDARD2_0_OR_GREATER
namespace Tgstation.Server.Common.Http
{
	/// <summary>
	/// Creates <see cref="IHttpClient"/>s.
	/// </summary>
	public interface IAbstractHttpClientFactory
	{
		/// <summary>
		/// Create a <see cref="IHttpClient"/>.
		/// </summary>
		/// <returns>A new <see cref="IHttpClient"/>.</returns>
		IHttpClient CreateClient();
	}
}
#endif
