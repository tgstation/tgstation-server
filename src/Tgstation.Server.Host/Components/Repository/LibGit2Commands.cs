using LibGit2Sharp;
using System;
using System.Collections.Generic;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class LibGit2Commands : ILibGit2Commands
	{
		/// <inheritdoc />
		public void Checkout(LibGit2Sharp.IRepository libGit2Repo, CheckoutOptions checkoutOptions, string commitish)
			=> Commands.Checkout(libGit2Repo, commitish, checkoutOptions);

		/// <inheritdoc />
		public void Fetch(
			LibGit2Sharp.IRepository libGit2Repo,
			IEnumerable<string> refSpecs,
			Remote remote,
			FetchOptions fetchOptions,
			string logMessage)
		{
			if (libGit2Repo == null)
				throw new ArgumentNullException(nameof(libGit2Repo));

			if (!(libGit2Repo is LibGit2Sharp.Repository concreteRepo))
				throw new ArgumentException("libGit2Repo must be an instance of LibGit2Sharp.Repository!", nameof(libGit2Repo));

			if (remote == null)
				throw new ArgumentNullException(nameof(remote));

			Commands.Fetch(concreteRepo, remote.Name, refSpecs, fetchOptions, logMessage);
		}
	}
}
