using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Helper for returning paginated models.
	/// </summary>
	/// <typeparam name="TModel">The <see cref="Type"/> of model intended to be returned.</typeparam>
	public sealed class PaginatableResult<TModel>
	{
		/// <summary>
		/// The <see cref="IQueryable{T}"/> <typeparamref name="TModel"/> results.
		/// </summary>
		public IQueryable<TModel> Results { get; }

		/// <summary>
		/// An <see cref="IActionResult"/> to return immediately.
		/// </summary>
		public IActionResult EarlyOut { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PaginatableResult{TModel}"/> <see langword="class"/>.
		/// </summary>
		/// <param name="results">The value of <see cref="Results"/>.</param>
		public PaginatableResult(IQueryable<TModel> results)
		{
			Results = results ?? throw new ArgumentNullException(nameof(results));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PaginatableResult{TModel}"/> <see langword="class"/>.
		/// </summary>
		/// <param name="earlyOut">The value of <see cref="EarlyOut"/>.</param>
		public PaginatableResult(IActionResult earlyOut)
		{
			EarlyOut = earlyOut ?? throw new ArgumentNullException(nameof(earlyOut));
		}
	}
}
