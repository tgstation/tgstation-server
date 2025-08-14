using System;
using System.Linq.Expressions;

using Tgstation.Server.Host.Authority.Core;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Contains a transformation <see cref="Expression"/> for converting <typeparamref name="TInput"/>s to <typeparamref name="TOutput"/>s.
	/// </summary>
	/// <typeparam name="TInput">The input <see cref="Type"/>.</typeparam>
	/// <typeparam name="TOutput">The output <see cref="Type"/>.</typeparam>
	public interface ITransformer<TInput, TOutput>
	{
		/// <summary>
		/// <see cref="Expression{TDelegate}"/> form of the transformation.
		/// </summary>
		Expression<Func<TInput, TOutput>> Expression { get; }

		/// <summary>
		/// <see cref="Expression{TDelegate}"/> for mapping <typeparamref name="TInput"/> into a <see cref="Projected{TQueried, TResult}"/>.
		/// </summary>
		Expression<Func<TInput, Projected<TInput, TOutput>>> ProjectedExpression { get; }

		/// <summary>
		/// The compiled <see cref="Expression"/>.
		/// </summary>
		Func<TInput, TOutput> CompiledExpression { get; }
	}
}
