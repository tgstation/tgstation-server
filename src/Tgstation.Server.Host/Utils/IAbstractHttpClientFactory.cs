using Tgstation.Server.Common;

namespace Tgstation.Server.Host.Utils
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
