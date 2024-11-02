using System.Threading;
using System.Threading.Tasks;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Security.OAuth
{
	/// <summary>
	/// Validates OAuth responses for a given <see cref="Provider"/>.
	/// </summary>
	public interface IOAuthValidator
	{
		/// <summary>
		/// The <see cref="OAuthProvider"/> this validator is for.
		/// </summary>
		OAuthProvider Provider { get; }

		/// <summary>
		/// The <see cref="OAuthGatewayStatus"/> for the <see cref="IOAuthValidator"/>.
		/// </summary>
		OAuthGatewayStatus GatewayStatus { get; }

		/// <summary>
		/// Gets the <see cref="OAuthProvider"/> of validator.
		/// </summary>
		/// <returns>The client ID of the validator on success, <see langword="null"/> on failure.</returns>
		OAuthProviderInfo GetProviderInfo();

		/// <summary>
		/// Validate a given OAuth response <paramref name="code"/>.
		/// </summary>
		/// <param name="code">The OAuth response string from web application.</param>
		/// <param name="requireUserID">If the resulting user ID should be retrieved.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in <see langword="null"/> if authentication failed or the validated <see cref="OAuthConnection.ExternalUserId"/> and OAuth access code otherwise.</returns>
		ValueTask<(string? UserID, string AccessCode)?> ValidateResponseCode(string code, bool requireUserID, CancellationToken cancellationToken);
	}
}
