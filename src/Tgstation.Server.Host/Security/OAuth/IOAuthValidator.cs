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
		/// The OAuth client ID of validator.
		/// </summary>
		string ClientId { get; }

		/// <summary>
		/// Validate a given OAuth response <paramref name="code"/>.
		/// </summary>
		/// <param name="code">The OAuth response string from web application.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="Task{TResult}"/> resulting in <see langword="null"/> if authentication failed, <see cref="global::System.UInt64.MaxValue"/> if a rate limit occurred, and the validated <see cref="OAuthConnection.ExternalUserId"/> otherwise.</returns>
		Task<ulong?> ValidateResponseCode(string code, CancellationToken cancellationToken);
	}
}
