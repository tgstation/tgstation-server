using System.Collections.Generic;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Contains <see cref="IOAuthValidator"/>s.
	/// </summary>
	public interface IOAuthProviders
	{
		/// <summary>
		/// Gets the <see cref="IOAuthValidator"/> for a given <paramref name="oAuthProvider"/>.
		/// </summary>
		/// <param name="oAuthProvider">The <see cref="OAuthProvider"/> to get the validator for.</param>
		/// <param name="forLogin">If the resulting <see cref="IOAuthValidator"/> will be used to authenticate a server login.</param>
		/// <returns>The <see cref="IOAuthValidator"/> for <paramref name="oAuthProvider"/>.</returns>
		IOAuthValidator? GetValidator(OAuthProvider oAuthProvider, bool forLogin);

		/// <summary>
		/// Gets a <see cref="Dictionary{TKey, TValue}"/> of the provider client IDs.
		/// </summary>
		/// <returns>A new <see cref="Dictionary{TKey, TValue}"/> of the active <see cref="OAuthProviderInfo"/>s.</returns>
		Dictionary<OAuthProvider, OAuthProviderInfo> ProviderInfos();
	}
}
