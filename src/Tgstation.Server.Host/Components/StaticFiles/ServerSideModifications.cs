namespace Tgstation.Server.Host.Components.StaticFiles
{
	/// <summary>
	/// Represents code modifications via configuration.
	/// </summary>
	public sealed class ServerSideModifications
	{
		/// <summary>
		/// If the target dme was completely overwitten.
		/// </summary>
		public bool TotalDmeOverwrite { get; }

		/// <summary>
		/// The #include line which should be added to the beginning of the .dme if any.
		/// </summary>
		public string? HeadIncludeLine { get; }

		/// <summary>
		/// The #include line which should be added to the end of the .dme if any.
		/// </summary>
		public string? TailIncludeLine { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ServerSideModifications"/> class.
		/// </summary>
		/// <param name="headIncludeLine">The value of <see cref="HeadIncludeLine"/>.</param>
		/// <param name="tailIncludeLine">The value of <see cref="TailIncludeLine"/>.</param>
		/// <param name="totalDmeOverwrite">The value of <see cref="TotalDmeOverwrite"/>.</param>
		public ServerSideModifications(string? headIncludeLine, string? tailIncludeLine, bool totalDmeOverwrite)
		{
			HeadIncludeLine = headIncludeLine;
			TailIncludeLine = tailIncludeLine;
			TotalDmeOverwrite = totalDmeOverwrite;
		}
	}
}
