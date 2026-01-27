using System.Threading;
using System.Threading.Tasks;

using LibGit2Sharp;
using LibGit2Sharp.Handlers;

using Tgstation.Server.Host.Jobs;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// For generating <see cref="CredentialsHandler"/>s.
	/// </summary>
	interface ICredentialsProvider
	{
		/// <summary>
		/// Generate a <see cref="CredentialsHandler"/> from a given <paramref name="username"/> and <paramref name="password"/>.
		/// </summary>
		/// <param name="remoteFeatures">The <see cref="IGitRemoteFeatures"/> for the repository in question.</param>
		/// <param name="username">The optional username to use in the <see cref="CredentialsHandler"/>.</param>
		/// <param name="password">The optional password to use in the <see cref="CredentialsHandler"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operation.</param>
		/// <returns>A <see cref="ValueTask{TResult}"/> resulting in a new <see cref="CredentialsHandler"/>.</returns>
		ValueTask<CredentialsHandler> GenerateCredentialsHandler(IGitRemoteFeatures remoteFeatures, string? username, string? password, CancellationToken cancellationToken);

		/// <summary>
		/// Rethrow the authentication failure message as a <see cref="JobException"/> if it is one.
		/// </summary>
		/// <param name="exception">The current <see cref="LibGit2SharpException"/>.</param>
		public void CheckBadCredentialsException(LibGit2SharpException exception);
	}
}
