namespace Tgstation.Server.Client
{
	/// <summary>
	/// Settings for a paginated request.
	/// </summary>
	public sealed class PaginationSettings
	{
		/// <summary>
		/// The size of a page. Defaults to server settings.
		/// </summary>
		public int? PageSize { get; set; }

		/// <summary>
		/// The offset to take from. Default 0.
		/// </summary>
		public int? Offset { get; set; }

		/// <summary>
		/// The maximum amount of items to retrieve. Default everything.
		/// </summary>
		/// <remarks>Results will be truncated if overflow occurs due to <see cref="PageSize"/>.</remarks>
		public int? RetrieveCount { get; set; }
	}
}
