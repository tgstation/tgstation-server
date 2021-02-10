using System;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a request to clone the repository.
	/// </summary>
	public sealed class RepositoryCloneRequest : RepositoryApiBase
	{
		/// <summary>
		/// The origin URL to clone.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public Uri? Origin { get; set; }

		/// <summary>
		/// If submodules should be recursively cloned.
		/// </summary>
		public bool? RecurseSubmodules { get; set; }
	}
}
