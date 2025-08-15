namespace Tgstation.Server.Host.Authority.Core
{
	/// <summary>
	/// DTO for moving database projected <see cref="object"/>s through the system.
	/// </summary>
	/// <typeparam name="TQueried">The originally queried <see cref="global::System.Type"/>.</typeparam>
	/// <typeparam name="TResult">The output DTO <see cref="global::System.Type"/>.</typeparam>
	public sealed class ProjectedPair<TQueried, TResult>
	{
		/// <summary>
		/// The originally queried <typeparamref name="TQueried"/>.
		/// </summary>
		public required TQueried Queried { get; init; }

		/// <summary>
		/// The output DTO <typeparamref name="TResult"/>.
		/// </summary>
		public required TResult Result { get; init; }
	}
}
