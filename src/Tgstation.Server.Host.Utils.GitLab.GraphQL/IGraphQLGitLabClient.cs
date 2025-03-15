using System;

namespace Tgstation.Server.Host.Utils.GitLab.GraphQL
{
	/// <summary>
	/// Wrapper for using a GitLab <see cref="IGraphQLClient"/>.
	/// </summary>
	public interface IGraphQLGitLabClient : IAsyncDisposable
	{
		/// <summary>
		/// Gets the underlying <see cref="IGraphQLClient"/>.
		/// </summary>
		IGraphQLClient GraphQL { get; }
	}
}
