using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using Microsoft.AspNetCore.Mvc;

namespace Tgstation.Server.Host.Controllers
{
	/// <summary>
	/// Helper for returning paginated models.
	/// </summary>
	/// <typeparam name="TModel">The <see cref="Type"/> of model intended to be returned.</typeparam>
	public sealed class PaginatableResult<TModel>
	{
		/// <summary>
		/// If the <see cref="PaginatableResult{TModel}"/> ran into an issue while retrieving results.
		/// </summary>
		[MemberNotNullWhen(true, nameof(EarlyOut))]
		[MemberNotNullWhen(false, nameof(Results))]
		public bool Failure => EarlyOut != null;

		/// <summary>
		/// The <see cref="IOrderedQueryable{T}"/> <typeparamref name="TModel"/> results.
		/// </summary>
		public IOrderedQueryable<TModel>? Results { get; }

		/// <summary>
		/// An <see cref="IActionResult"/> to return immediately.
		/// </summary>
		public IActionResult? EarlyOut { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PaginatableResult{TModel}"/> class.
		/// </summary>
		/// <param name="results">The value of <see cref="Results"/>.</param>
		public PaginatableResult(IOrderedQueryable<TModel> results)
		{
			Results = results ?? throw new ArgumentNullException(nameof(results));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="PaginatableResult{TModel}"/> class.
		/// </summary>
		/// <param name="earlyOut">The value of <see cref="EarlyOut"/>.</param>
		public PaginatableResult(IActionResult earlyOut)
		{
			EarlyOut = earlyOut ?? throw new ArgumentNullException(nameof(earlyOut));
		}
	}
}
