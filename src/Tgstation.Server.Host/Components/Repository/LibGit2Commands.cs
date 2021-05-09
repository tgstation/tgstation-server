using System;
using System.Collections.Generic;

using LibGit2Sharp;

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

			if (remote == null)
				throw new ArgumentNullException(nameof(remote));

			Commands.Fetch((LibGit2Sharp.Repository)libGit2Repo, remote.Name, refSpecs, fetchOptions, logMessage);
		}
	}
}
