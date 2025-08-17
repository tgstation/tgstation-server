using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// A projectable <typeparamref name="TResult"/> based on an underlying <typeparamref name="TQueried"/>.
	/// </summary>
	/// <typeparam name="TQueried">The <see cref="EntityId"/> model <see cref="Type"/> queried.</typeparam>
	/// <typeparam name="TResult">The transformed result <see cref="Type"/>.</typeparam>
	public sealed class Projectable<TQueried, TResult>
		where TQueried : EntityId
		where TResult : notnull
	{
		/// <summary>
		/// The underlying <see cref="IQueryable{T}"/>. Should only select one entity.
		/// </summary>
		readonly IQueryable<TQueried> query;

		/// <summary>
		/// The selector for the <see cref="ProjectedPair{TQueried, TResult}"/> data required by the <see cref="resultMapper"/>.
		/// </summary>
		readonly Expression<Func<ProjectedPair<TQueried, TResult>, ProjectedPair<object?, TResult>>> selector;

		/// <summary>
		/// Mapper for transforming the <see cref="ProjectedPair{TQueried, TResult}"/> <typeparamref name="TResult"/> into an <see cref="AuthorityResponse{TResult}"/>. Called with the output of <see cref="selector"/> as <see cref="ProjectedPair{TQueried, TResult}.Queried"/>.
		/// </summary>
		readonly Func<ProjectedPair<object?, TResult>?, AuthorityResponse<TResult>> resultMapper;

		/// <summary>
		/// <see cref="CancellationToken"/> for the operation.
		/// </summary>
		readonly CancellationToken cancellationToken;

		/// <summary>
		/// Combine a set of <see cref="Projectable{TQueried, TResult}"/>s into the resulting <see cref="AuthorityResponse{TResult}"/>s.
		/// </summary>
		/// <param name="projection">The projection used to <see cref="Resolve(Func{IQueryable{TQueried}, IQueryable{ProjectedPair{TQueried, TResult}}})"/> each of the <paramref name="inputs"/>.</param>
		/// <param name="inputs">The <see cref="Projectable{TQueried, TResult}"/>s to combine.</param>
		/// <returns>A <see cref="Dictionary{TKey, TValue}"/> of the resulting <see cref="AuthorityResponse{TResult}"/>s keyed by their <see cref="EntityId.Id"/>.</returns>
		public static async ValueTask<Dictionary<long, AuthorityResponse<TResult>>> Combine(Func<IQueryable<TQueried>, IQueryable<ProjectedPair<TQueried, TResult>>> projection, params Projectable<TQueried, TResult>[] inputs)
		{
			ArgumentNullException.ThrowIfNull(projection);
			ArgumentNullException.ThrowIfNull(inputs);

			if (inputs.Length == 0)
				return new Dictionary<long, AuthorityResponse<TResult>>();

			var firstProjectable = inputs[0];
			var workingSet = projection(firstProjectable.query);
			foreach (var projectable in inputs.Skip(1))
			{
				if (firstProjectable.resultMapper != projectable.resultMapper)
					throw new InvalidOperationException($"Different implementations of {nameof(resultMapper)} in combined {firstProjectable.GetType().Name}.");
				if (firstProjectable.selector != projectable.selector)
					throw new InvalidOperationException($"Different implementations of {nameof(selector)} in combined {firstProjectable.GetType().Name}.");

				workingSet = workingSet.Union(projection(projectable.query));
			}

			using var cts = CancellationTokenSource.CreateLinkedTokenSource(inputs.Select(projectable => projectable.cancellationToken).ToArray());
			var finalQueryable = workingSet
				.Select(CombinedProjectionExpression(firstProjectable.selector));

			return await finalQueryable
				.ToDictionaryAsync(
					result => result.Id,
					result => firstProjectable.resultMapper(result.Projected));
		}

		/// <summary>
		/// Create a <see cref="Projectable{TQueried, TResult}"/>.
		/// </summary>
		/// <param name="query">The underlying <see cref="IQueryable{T}"/>.</param>
		/// <param name="resultMapper">A mapper for taking a new <see cref="ProjectedPair{TQueried, TResult}"/> (Who's <see cref="ProjectedPair{TQueried, TResult}.Queried"/> will be <see langword="null"/>) with its <see cref="ProjectedPair{TQueried, TResult}.Result"/> set amd converting it into an <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operations on <paramref name="query"/>.</param>
		/// <returns>A new <see cref="Projectable{TQueried, TResult}"/>.</returns>
		public static Projectable<TQueried, TResult> Create(
			IQueryable<TQueried> query,
			Func<ProjectedPair<object?, TResult>?, AuthorityResponse<TResult>> resultMapper,
			CancellationToken cancellationToken)
			=> Create(
				query,
				projected => new ProjectedPair<object?, TResult>
				{
					Queried = null,
					Result = projected.Result,
				},
				resultMapper,
				cancellationToken);

		/// <summary>
		/// Create a <see cref="Projectable{TQueried, TResult}"/>.
		/// </summary>
		/// <typeparam name="TSelection">A <see cref="Type"/> selected out of <typeparamref name="TQueried"/> to be used in <paramref name="resultMapper"/>.</typeparam>
		/// <param name="query">The underlying <see cref="IQueryable{T}"/>.</param>
		/// <param name="selector">Selects a value to be set as the <see cref="ProjectedPair{TQueried, TResult}.Queried"/> value for later use in <paramref name="resultMapper"/>.</param>
		/// <param name="resultMapper">A mapper for taking a new <see cref="ProjectedPair{TQueried, TResult}"/> (Who's <see cref="ProjectedPair{TQueried, TResult}.Queried"/> will be the result of <paramref name="selector"/> on the original <paramref name="query"/>.) with its <see cref="ProjectedPair{TQueried, TResult}.Result"/> set amd converting it into an <see cref="AuthorityResponse{TResult}"/>.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/> for the operations on <paramref name="query"/>.</param>
		/// <returns>A new <see cref="Projectable{TQueried, TResult}"/>.</returns>
		public static Projectable<TQueried, TResult> Create<TSelection>(
			IQueryable<TQueried> query,
			Expression<Func<ProjectedPair<TQueried, TResult>, ProjectedPair<TSelection, TResult>>> selector,
			Func<ProjectedPair<TSelection, TResult>?, AuthorityResponse<TResult>> resultMapper,
			CancellationToken cancellationToken)
		{
			Expression<Func<ProjectedPair<TSelection, TResult>, ProjectedPair<object?, TResult>>> makeProjectedGeneric = projected => new ProjectedPair<object?, TResult>
			{
				Queried = projected.Queried,
				Result = projected.Result,
			};

			var parameter = Expression.Parameter(typeof(ProjectedPair<TQueried, TResult>), "innerProjectedParam");
			return new(
				query,
				Expression.Lambda<Func<ProjectedPair<TQueried, TResult>, ProjectedPair<object?, TResult>>>(
					Expression.Invoke(
						makeProjectedGeneric,
						Expression.Invoke(
							selector,
							parameter)),
					parameter),
				projected => resultMapper(
					projected != null
						? new ProjectedPair<TSelection, TResult>
						{
							Queried = (TSelection)projected.Queried!,
							Result = projected.Result,
						}
						: null),
				cancellationToken);
		}

		/// <summary>
		/// Create an <see cref="Expression{TDelegate}"/> to transform a <see cref="ProjectedPair{TQueried, TResult}"/> into a <see cref="CombinedProjection"/>.
		/// </summary>
		/// <param name="selector">The selector used for to get business data for the <see cref="resultMapper"/>.</param>
		/// <returns>A new <see cref="Expression{TDelegate}"/> transforming a <see cref="ProjectedPair{TQueried, TResult}"/> into a <see cref="CombinedProjection"/>.</returns>
		private static Expression<Func<ProjectedPair<TQueried, TResult>, CombinedProjection>> CombinedProjectionExpression(Expression<Func<ProjectedPair<TQueried, TResult>, ProjectedPair<object?, TResult>>> selector)
		{
			Expression<Func<ProjectedPair<TQueried, TResult>, long>> idSelector = projected => projected.Queried.Id!.Value;

			var parameter = Expression.Parameter(typeof(ProjectedPair<TQueried, TResult>), "projectedParamForCombined");
			var selection = Expression.Invoke(selector, parameter);
			var id = Expression.Invoke(idSelector, parameter);

			var ourType = typeof(CombinedProjection);

			var memberInitExpr = Expression.MemberInit(
				Expression.New(ourType),
				Expression.Bind(
					ourType.GetProperty(nameof(CombinedProjection.Id))!,
					id),
				Expression.Bind(
					ourType.GetProperty(nameof(CombinedProjection.Projected))!,
					selection));

			var finalExpr = Expression.Lambda<Func<ProjectedPair<TQueried, TResult>, CombinedProjection>>(memberInitExpr, parameter);

			return finalExpr;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Projectable{TQueried, TResult}"/> class.
		/// </summary>
		/// <param name="query">The value of <see cref="query"/>.</param>
		/// <param name="selector">The value of <see cref="selector"/>.</param>
		/// <param name="resultMapper">The value of <see cref="resultMapper"/>.</param>
		/// <param name="cancellationToken">The value of <see cref="cancellationToken"/>.</param>
		private Projectable(
			IQueryable<TQueried> query,
			Expression<Func<ProjectedPair<TQueried, TResult>, ProjectedPair<object?, TResult>>> selector,
			Func<ProjectedPair<object?, TResult>?, AuthorityResponse<TResult>> resultMapper,
			CancellationToken cancellationToken)
		{
			this.query = query ?? throw new ArgumentNullException(nameof(query));
			this.selector = selector ?? throw new ArgumentNullException(nameof(selector));
			this.resultMapper = resultMapper ?? throw new ArgumentNullException(nameof(resultMapper));
			this.cancellationToken = cancellationToken;
		}

		/// <summary>
		/// Resolve the <see cref="Projectable{TQueried, TResult}"/>.
		/// </summary>
		/// <param name="projection">The mapping from <typeparamref name="TQueried"/> to <typeparamref name="TResult"/>.</param>
		/// <returns>The resolved <see cref="AuthorityResponse{TResult}"/>.</returns>
		public async ValueTask<AuthorityResponse<TResult>> Resolve(Func<IQueryable<TQueried>, IQueryable<ProjectedPair<TQueried, TResult>>> projection)
		{
			ArgumentNullException.ThrowIfNull(projection);
			var finalQueryable = projection(query)
				.Select(selector);
			var selection = await finalQueryable
				.FirstOrDefaultAsync(cancellationToken);
			return resultMapper(selection);
		}

		/// <summary>
		/// DTO for selecting the <see cref="Id"/> from <typeparamref name="TQueried"/> alongside the selection <see cref="ProjectedPair{TQueried, TResult}"/>.
		/// </summary>
		private class CombinedProjection
		{
			/// <summary>
			/// The <see cref="EntityId.Id"/> of <typeparamref name="TQueried"/>.
			/// </summary>
			public required long Id { get; init; }

			/// <summary>
			/// The selection/<typeparamref name="TResult"/> <see cref="ProjectedPair{TQueried, TResult}"/>.
			/// </summary>
			public required ProjectedPair<object?, TResult> Projected { get; init; }
		}
	}
}
