using System;
using System.ComponentModel.DataAnnotations;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class OAuthConnection : Api.Models.OAuthConnection, IApiTransformable<Api.Models.OAuthConnection>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The owning <see cref="Models.User"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(user))]
		public User User
		{
			get => user ?? throw new InvalidOperationException("User not set!");
			set => user = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.OAuthConnection.ExternalUserId"/>.
		/// </summary>
		[StringLength(Limits.MaximumIndexableStringLength, MinimumLength = 1)]
		public new string ExternalUserId
		{
			get => base.ExternalUserId ?? throw new InvalidOperationException("ExternalUserId was null!");
			set => base.ExternalUserId = value;
		}

		/// <summary>
		/// Backing field for <see cref="User"/>.
		/// </summary>
		User? user;

		/// <inheritdoc />
		public Api.Models.OAuthConnection ToApi() => new ()
		{
			Provider = Provider,
			ExternalUserId = ExternalUserId,
		};
	}
}
