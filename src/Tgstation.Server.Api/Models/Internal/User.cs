using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a server <see cref="User"/>
	/// </summary>
	public class User
	{
		/// <summary>
		/// The ID of the <see cref="User"/>
		/// </summary>
		[Required]
		public long? Id { get; set; }

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

		/// <summary>
		/// The name of the <see cref="User"/>
		/// </summary>
		[Required]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? Name { get; set; }
	}
}
