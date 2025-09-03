using System.Collections.Generic;
using System.Linq;

namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// Represents a database table.
	/// </summary>
	/// <typeparam name="TModel">The type of model.</typeparam>
	public interface IDatabaseCollection<TModel> : IQueryable<TModel>
	{
		/// <summary>
		/// An <see cref="IEnumerable{T}"/> of <typeparamref name="TModel"/>s prioritizing in the working set.
		/// </summary>
		IEnumerable<TModel> Local { get; }

		/// <summary>
		/// Add a given <paramref name="model"/> to the the working set.
		/// </summary>
		/// <param name="model">The <typeparamref name="TModel"/> model to add.</param>
		void Add(TModel model);

		/// <summary>
		/// Remove a given <paramref name="model"/> from the the working set.
		/// </summary>
		/// <param name="model">The <typeparamref name="TModel"/> model to remove.</param>
		void Remove(TModel model);

		/// <summary>
		/// Attach a given <paramref name="model"/> to the the working set.
		/// </summary>
		/// <param name="model">The <typeparamref name="TModel"/> model to add.</param>
		void Attach(TModel model);

		/// <summary>
		/// Add a range of <paramref name="models"/> to the <see cref="IDatabaseCollection{TModel}"/>.
		/// </summary>
		/// <param name="models">An <see cref="IEnumerable{T}"/> of <typeparamref name="TModel"/>s to add.</param>
		void AddRange(IEnumerable<TModel> models);

		/// <summary>
		/// Remove a range of <paramref name="models"/> from the <see cref="IDatabaseCollection{TModel}"/>.
		/// </summary>
		/// <param name="models">An <see cref="IEnumerable{T}"/> of <typeparamref name="TModel"/>s to remove.</param>
		void RemoveRange(IEnumerable<TModel> models);
	}
}
