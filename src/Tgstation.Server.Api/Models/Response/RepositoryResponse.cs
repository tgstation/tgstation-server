using System;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a git repository
	/// </summary>
	public sealed class RepositoryResponse : RepositoryApiBase, IGitRemoteInformation
	{
		/// <summary>
		/// The origin URL. If <see langword="null"/>, the git repository does not currently exist on the server.
		/// </summary>
		[ResponseOptions]
		public Uri? Origin { get; set; }

		/// <summary>
		/// The current <see cref="Models.RevisionInformation"/>.
		/// </summary>
		[ResponseOptions]
		public RevisionInformation? RevisionInformation { get; set; }

		/// <inheritdoc />
		[ResponseOptions]
		public RemoteGitProvider? RemoteGitProvider { get; set; }

		/// <inheritdoc />
		[ResponseOptions]
		public string? RemoteRepositoryOwner { get; set; }

		/// <inheritdoc />
		[ResponseOptions]
		public string? RemoteRepositoryName { get; set; }

		/// <summary>
		/// The <see cref="JobResponse"/> started by the request, if any.
		/// </summary>
		[ResponseOptions]
		public JobResponse? ActiveJob { get; set; }
	}
}
