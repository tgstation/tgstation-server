namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Sanity limits to prevent users from overloading
	/// </summary>
	public static class Limits
	{
		/// <summary>
		/// Length limit for strings in fields.
		/// </summary>
		public const int MaximumStringLength = 10000;
	}
}