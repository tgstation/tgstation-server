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

		/// <summary>
		/// The <see cref="Api.HeadersException"/> thrown when attempting to parse the <see cref="ApiHeaders"/> if any.
		/// </summary>
		HeadersException HeadersException { get; }

		/// <summary>
		/// Attempt to create <see cref="Api.ApiHeaders"/> without checking for the presence of an <see cref="Microsoft.Net.Http.Headers.HeaderNames.Authorization"/> header.
		/// </summary>
		/// <returns>A new <see cref="Api.ApiHeaders"/> <see langword="class"/>.</returns>
		/// <remarks>This does not populate the <see cref="ApiHeaders"/> property.</remarks>
		/// <exception cref="Api.HeadersException">Thrown if the requested <see cref="Api.ApiHeaders"/> contain errors other than <see cref="HeaderErrorTypes.AuthorizationMissing"/>.</exception>
		ApiHeaders CreateAuthlessHeaders();
	}
}
