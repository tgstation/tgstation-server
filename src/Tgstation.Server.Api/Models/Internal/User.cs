using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a server <see cref="User"/>
	/// </summary>
	public abstract class User : UserBase
	{
		/// <summary>
		/// If the <see cref="User"/> is enabled since users cannot be deleted. System users cannot be disabled
		/// </summary>
		[Required]
		public bool? Enabled { get; set; }

		/// <summary>
		/// When the <see cref="User"/> was created
		/// </summary>
		[Required]
		public DateTimeOffset? CreatedAt { get; set; }

		/// <summary>
		/// The SID/UID of the <see cref="User"/> on Windows/POSIX respectively
		/// </summary>
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? SystemIdentifier { get; set; }
	}
}
