namespace Tgstation.Server.Client.GraphQL
{
	/// <summary>
	/// A <see cref="IGraphQLServerClient"/> known to be authenticated.
	/// </summary>
	public interface IAuthenticatedGraphQLServerClient : IGraphQLServerClient
	{
		/// <summary>
		/// The REST <see cref="ITransferClient"/>.
		/// </summary>
		ITransferClient TransferClient { get; }
	}
}
