using System;

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

		/// <summary>
		/// Length limit for <see cref="Internal.ChatBot.Name"/>s.
		/// </summary>
		public const int MaximumIndexableStringLength = 100;

		/// <summary>
		/// Length limit for git commit SHAs.
		/// </summary>
		public const int MaximumCommitShaLength = 40;

		/// <summary>
		/// The maximum size for file transfers.
		/// </summary>
		public const int MaximumFileTransferSize = Int32.MaxValue;
	}
}
