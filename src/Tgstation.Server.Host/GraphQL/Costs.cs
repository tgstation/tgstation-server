namespace Tgstation.Server.Host.GraphQL
{
	/// <summary>
	/// Values used with <see cref="HotChocolate.CostAnalysis.Types.CostAttribute"/> for when the default is insufficient.
	/// </summary>
	static class Costs
	{
		/// <summary>
		/// Cost for non-ById <see cref="global::System.Linq.IQueryable{T}"/> queries.
		/// </summary>
		public const int NonIndexedQueryable = 100;
	}
}
