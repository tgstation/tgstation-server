using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using TGS.Server.Components;

namespace TGS.Server
{
	/// <inheritdoc />
	sealed class RepositoryProvider : IRepositoryProvider
	{
		/// <inheritdoc />
		public event ProgressHandler OnProgress;
		/// <inheritdoc />
		public event TransferProgressHandler OnTransferProgress;
		/// <inheritdoc />
		public event CheckoutProgressHandler OnCheckoutProgress;

		/// <inheritdoc />
		public CredentialsHandler CredentialsProvider { get; set; }

		/// <summary>
		/// The <see cref="IIOManager"/> for the <see cref="RepositoryProvider"/>
		/// </summary>
		readonly IIOManager IO;

		/// <summary>
		/// Create <see cref="FetchOptions"/> that Prune and have the appropriate credentials and progress handler
		/// </summary>
		/// <returns>Properly configured <see cref="FetchOptions"/></returns>
		FetchOptions GenerateFetchOptions()
		{
			return new FetchOptions()
			{
				CredentialsProvider = CredentialsProvider,
				OnTransferProgress = (a) => OnTransferProgress(a),
				OnProgress = (a) => OnProgress(a),
				Prune = true,
			};
		}

		/// <summary>
		/// Construct a <see cref="RepositoryProvider"/>
		/// </summary>
		/// <param name="io">The value of <see cref="IO"/></param>
		public RepositoryProvider(IIOManager io)
		{
			IO = io;
		}

		/// <inheritdoc />
		public string Checkout(IRepository repositoryToCheckout, string targetObject)
		{
			try
			{
				Commands.Checkout((Repository)repositoryToCheckout, targetObject, new CheckoutOptions()
				{
					CheckoutModifiers = CheckoutModifiers.Force,
					OnCheckoutProgress = (a, b, c) => OnCheckoutProgress(a, b, c),
				});
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public string Fetch(IRepository repository)
		{
			try
			{
				string logMessage = "";
				var R = repository.Network.Remotes[RepositoryManager.DefaultRemote];
				IEnumerable<string> refSpecs = R.FetchRefSpecs.Select(X => X.Specification);
				Commands.Fetch((Repository)repository, R.Name, refSpecs, GenerateFetchOptions(), logMessage);	//unsafe cast is fine
				return null;
			}
			catch (Exception e)
			{
				return e.ToString();
			}
		}

		/// <inheritdoc />
		public bool IsValid(string path)
		{
			return Repository.IsValid(IO.ResolvePath(path));
		}

		/// <inheritdoc />
		public IRepository LoadRepository(string path)
		{
			return new Repository(IO.ResolvePath(path));
		}

		/// <inheritdoc />
		public string Stage(IRepository repository, string path)
		{
			path = IO.ResolvePath(path);
			try
			{
				Commands.Stage(repository, path);
				return null;
			}
			catch(Exception e)
			{
				return e.ToString();
			}
		}
	}
}
