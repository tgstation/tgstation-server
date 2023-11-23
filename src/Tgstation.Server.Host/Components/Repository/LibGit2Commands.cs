using System;
using System.Collections.Generic;

using LibGit2Sharp;

#nullable disable

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
			ArgumentNullException.ThrowIfNull(libGit2Repo);

			ArgumentNullException.ThrowIfNull(remote);

			Commands.Fetch((LibGit2Sharp.Repository)libGit2Repo, remote.Name, refSpecs, fetchOptions, logMessage);
		}
	}
}
