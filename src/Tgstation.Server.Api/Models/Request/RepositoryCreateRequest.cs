using System;

using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models.Request
{
	/// <summary>
	/// Represents a request to clone the repository.
	/// </summary>
	public sealed class RepositoryCreateRequest : RepositoryApiBase
	{
		/// <summary>
		/// The origin URL to clone.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public Uri? Origin { get; set; }

		/// <summary>
		/// If submodules should be recursively cloned. Note that further updates are not recursive.
		/// </summary>
		[Obsolete("Use updateSubmodules instead.")]
		public bool? RecurseSubmodules { get; set; }
	}
}
