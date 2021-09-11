using System;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents a git repository.
	/// </summary>
	public sealed class RepositoryResponse : RepositoryApiBase
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

		/// <summary>
		/// The current <see cref="Models.GitRemoteInformation"/>.
		/// </summary>
		[ResponseOptions]
		public GitRemoteInformation? GitRemoteInformation { get; set; }

		/// <summary>
		/// The <see cref="JobResponse"/> started by the request, if any.
		/// </summary>
		[ResponseOptions]
		public JobResponse? ActiveJob { get; set; }
	}
}
