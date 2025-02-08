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
		/// <example>5</example>
		public int TotalPages { get; set; }

		/// <summary>
		/// The current size of pages in the query.
		/// </summary>
		/// <example>20</example>
		public int PageSize { get; set; }

		/// <summary>
		/// The total items across all pages.
		/// </summary>
		/// <example>100</example>
		public int TotalItems { get; set; }
	}
}
