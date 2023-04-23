namespace Tgstation.Server.Common
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
