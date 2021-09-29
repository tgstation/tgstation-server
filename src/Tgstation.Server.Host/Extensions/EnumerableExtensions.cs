using System;
using System.Collections.Generic;
using System.Linq;

namespace Tgstation.Server.Host.Extensions
{
	/// <summary>
	/// Extension methods for <see cref="IEnumerable{T}"/>.
	/// </summary>
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Filters <see langword="null"/> references from a given <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <typeparam name="T">The <see langword="class"/> <see cref="Type"/> of the <paramref name="enumerable"/>.</typeparam>
		/// <param name="enumerable">The <see cref="IEnumerable{T}"/> of nullable references to filter.</param>
		/// <returns><paramref name="enumerable"/> with <see langword="null"/> references filtered out and properly typed.</returns>
		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : class
		{
			if (enumerable == null)
				throw new ArgumentNullException(nameof(enumerable));
			return enumerable.Where(e => e != null).Select(e => e!);
		}

		/// <summary>
		/// Filters <see langword="null"/> references from a given <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <typeparam name="T">The <see langword="struct"/> <see cref="Type"/> of the <paramref name="enumerable"/>.</typeparam>
		/// <param name="enumerable">The <see cref="IEnumerable{T}"/> of nullable references to filter.</param>
		/// <returns><paramref name="enumerable"/> with <see langword="null"/> references filtered out and properly typed.</returns>
		public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> enumerable) where T : struct
		{
			if (enumerable == null)
				throw new ArgumentNullException(nameof(enumerable));
			return enumerable.Where(e => e.HasValue).Select(e => e!.Value);
		}
	}
}
