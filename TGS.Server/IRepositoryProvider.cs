﻿using LibGit2Sharp;
using LibGit2Sharp.Handlers;

namespace TGS.Server
{
	/// <summary>
	/// <see langword="interface"/> for managing a <see cref="Repository"/>
	/// </summary>
	interface IRepositoryProvider
	{
		/// <summary>
		/// Used for <see cref="FetchOptionsBase.OnProgress"/>
		/// </summary>
		event ProgressHandler OnProgress;

		/// <summary>
		/// Used for <see cref="FetchOptionsBase.OnTransferProgress"/>
		/// </summary>
		event TransferProgressHandler OnTransferProgress;
		
		/// <summary>
		/// Used for <see cref="FetchOptionsBase.CredentialsProvider"/>
		/// </summary>
		CredentialsHandler CredentialsProvider { get; set; }

		/// <summary>
		/// Fetches origin of <see cref="Repository"/>
		/// </summary>
		/// <param name="repositoryToFetch">The <see cref="IRepository"/> to fetch commits for</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Fetch(IRepository repositoryToFetch);

		/// <summary>
		/// Checks if a <see cref="IRepository"/> can be created from <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to check for a <see cref="IRepository"/></param>
		/// <returns><see langword="true"/> if <paramref name="path"/> is a valid <see cref="IRepository"/>, <see langword="false"/> otherwise</returns>
		bool IsValid(string path);

		/// <summary>
		/// Returns the provided <see cref="IRepository"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="IRepository"/></param>
		IRepository LoadRepository(string path);
	}
}
