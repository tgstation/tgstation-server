using LibGit2Sharp.Handlers;

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
		/// <param name="username">The optional username to use in the <see cref="CredentialsHandler"/>.</param>
		/// <param name="password">The optional password to use in the <see cref="CredentialsHandler"/>.</param>
		/// <returns>A new <see cref="CredentialsHandler"/>.</returns>
		CredentialsHandler GenerateCredentialsHandler(string? username, string? password);
	}
}
