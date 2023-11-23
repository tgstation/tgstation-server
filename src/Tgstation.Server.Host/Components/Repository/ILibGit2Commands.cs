using System.Collections.Generic;

using LibGit2Sharp;

#nullable disable

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// For low level interactions with a <see cref="LibGit2Sharp.IRepository"/>.
	/// </summary>
	interface ILibGit2Commands
	{
		/// <summary>
		/// Runs a blocking fetch operation on a given <paramref name="repository"/>.
		/// </summary>
		/// <param name="repository">The <see cref="IRepository"/> to fetch with.</param>
		/// <param name="refSpecs">The refspecs to fetch.</param>
		/// <param name="remote">The <see cref="Remote"/> to fetch.</param>
		/// <param name="fetchOptions">The <see cref="FetchOptions"/>.</param>
		/// <param name="logMessage">The message to write in the git log.</param>
		void Fetch(LibGit2Sharp.IRepository repository, IEnumerable<string> refSpecs, Remote remote, FetchOptions fetchOptions, string logMessage);

		/// <summary>
		/// Runs a blocking checkout operation on a given <paramref name="repository"/>.
		/// </summary>
		/// <param name="repository">The <see cref="IRepository"/> to checkout with.</param>
		/// <param name="checkoutOptions">The <see cref="CheckoutOptions"/>.</param>
		/// <param name="commitish">The git object to checkout.</param>
		void Checkout(LibGit2Sharp.IRepository repository, CheckoutOptions checkoutOptions, string commitish);
	}
}
