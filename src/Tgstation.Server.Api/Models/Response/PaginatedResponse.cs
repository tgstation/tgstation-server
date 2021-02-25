using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Response
{
	/// <summary>
	/// Represents a paginated set of models.
	/// </summary>
	/// <typeparam name="TModel">The <see cref="System.Type"/> of the returned model.</typeparam>
	public sealed class PaginatedResponse<TModel>
	{
		/// <summary>
		/// The <see cref="ICollection{T}"/> of the returned <typeparamref name="TModel"/>s.
		/// </summary>
		[Required]
		public ICollection<TModel>? Content { get; set; }

		/// <summary>
		/// The total number of pages in the query.
		/// </summary>
		public int TotalPages { get; set; }

		/// <summary>
		/// The current size of pages in the query.
		/// </summary>
		public int PageSize { get; set; }
	}
}
