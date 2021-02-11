using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a server user.
	/// </summary>
	public abstract class UserModelBase : UserName
	{
		/// <summary>
		/// If the <see cref="UserModelBase"/> is enabled since users cannot be deleted. System users cannot be disabled
		/// </summary>
		[Required]
		public bool? Enabled { get; set; }

		/// <summary>
		/// When the <see cref="UserModelBase"/> was created
		/// </summary>
		[Required]
		[RequestOptions(FieldPresence.Ignored)]
		public DateTimeOffset? CreatedAt { get; set; }

		/// <summary>
		/// The SID/UID of the <see cref="UserModelBase"/> on Windows/POSIX respectively
		/// </summary>
		[ResponseOptions]
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public string? SystemIdentifier { get; set; }
	}
}
