using System;

namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents a server log file.
	/// </summary>
	public sealed class LogFile : FileTicketResult
	{
		/// <summary>
		/// The name of the log file.
		/// </summary>
		public string Name { get; set; } = String.Empty;

		/// <summary>
		/// The <see cref="DateTimeOffset"/> of when the log file was modified.
		/// </summary>
		public DateTimeOffset LastModified { get; set; }
	}
}
