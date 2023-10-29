using Tgstation.Server.Api;

namespace Tgstation.Server.Host.Utils
{
	/// <summary>
	/// Provides <see cref="ApiHeaders"/>.
	/// </summary>
	public interface IApiHeadersProvider
	{
		/// <summary>
		/// The created <see cref="Api.ApiHeaders"/>, if any.
		/// </summary>
		ApiHeaders ApiHeaders { get; }
	}
}
