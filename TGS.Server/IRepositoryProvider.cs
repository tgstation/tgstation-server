using LibGit2Sharp;
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
		/// Used for <see cref="CheckoutOptions.OnCheckoutProgress"/>
		/// </summary>
		event CheckoutProgressHandler OnCheckoutProgress;
		
		/// <summary>
		/// Used for <see cref="FetchOptionsBase.CredentialsProvider"/>
		/// </summary>
		CredentialsHandler CredentialsProvider { get; set; }

		/// <summary>
		/// Force checks out <paramref name="targetObject"/> on <paramref name="repositoryToCheckout"/>
		/// </summary>
		/// <param name="repositoryToCheckout">The <see cref="IRepository"/> to checkout on</param>
		/// <param name="targetObject">The commitish to checkout</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Checkout(IRepository repositoryToCheckout, string targetObject);

		/// <summary>
		/// Fetches origin of a <paramref name="repository"/>
		/// </summary>
		/// <param name="repository">The <see cref="IRepository"/> to fetch commits for</param>
		/// <returns><see langword="null"/> on success, error message on failure</returns>
		string Fetch(IRepository repository);

		/// <summary>
		/// Checks if a <see cref="IRepository"/> can be created from <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to check for a <see cref="IRepository"/></param>
		/// <returns><see langword="true"/> if <paramref name="path"/> is a valid <see cref="IRepository"/>, <see langword="false"/> otherwise</returns>
		bool IsValid(string path);

		/// <summary>
		/// Returns a <see cref="IRepository"/> initialized from <paramref name="path"/>
		/// </summary>
		/// <param name="path">The path to the <see cref="IRepository"/></param>
		IRepository LoadRepository(string path);

		/// <summary>
		/// Stage a <paramref name="path"/> for commiting to a <paramref name="repository"/>
		/// </summary>
		/// <param name="repository"></param>
		/// <param name="path"></param>
		/// <returns></returns>
		string Stage(IRepository repository, string path);
	}
}
