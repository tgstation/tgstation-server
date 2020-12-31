using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Contains <see cref="IOAuthValidator"/>s
	/// </summary>
	public interface IOAuthProviders
	{
		/// <summary>
		/// Gets the <see cref="IOAuthValidator"/> for a given <paramref name="oAuthProvider"/>.
		/// </summary>
		/// <param name="oAuthProvider">The <see cref="OAuthProvider"/> to get the validator for.</param>
		/// <returns>The <see cref="IOAuthValidator"/> for <paramref name="oAuthProvider"/>.</returns>
		IOAuthValidator GetValidator(OAuthProvider oAuthProvider);

		/// <summary>
		/// Gets a <see cref="Dictionary{TKey, TValue}"/> of the provider client IDs.
		/// </summary>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in a anew <see cref="Dictionary{TKey, TValue}"/> of the active <see cref="OAuthProviderInfo"/>s.</returns>
		Task<Dictionary<OAuthProvider, OAuthProviderInfo>> ProviderInfos(CancellationToken cancellationToken);
	}
}
