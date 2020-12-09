using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class User : Api.Models.Internal.User
	{
		/// <summary>
		/// Username used when creating jobs automatically.
		/// </summary>
		public const string TgsSystemUserName = "TGS";

		/// <summary>
		/// The hash of the user's password
		/// </summary>
		public string PasswordHash { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.User"/>
		/// </summary>
		public User CreatedBy { get; set; }

		/// <summary>
		/// The uppercase invariant of <see cref="Api.Models.Internal.User.Name"/>
		/// </summary>
		[Required]
		public string CanonicalName { get; set; }

		/// <summary>
		/// When <see cref="PasswordHash"/> was last changed
		/// </summary>
		public DateTimeOffset? LastPasswordUpdate { get; set; }

		/// <summary>
		/// <see cref="User"/>s created by this <see cref="User"/>
		/// </summary>
		public ICollection<User> CreatedUsers { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s for the <see cref="User"/>
		/// </summary>
		public ICollection<InstanceUser> InstanceUsers { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>
		/// </summary>
		public ICollection<TestMerge> TestMerges { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>
		/// </summary>
		public ICollection<OAuthConnection> OAuthConnections { get; set; }

		/// <summary>
		/// Change a <see cref="Api.Models.Internal.User.Name"/> into a <see cref="CanonicalName"/>.
		/// </summary>
		/// <param name="name">The <see cref="Api.Models.Internal.User.Name"/>.</param>
		/// <returns>The <see cref="CanonicalName"/>.</returns>
		public static string CanonicalizeName(string name) => name?.ToUpperInvariant() ?? throw new ArgumentNullException(nameof(name));

		/// <summary>
		/// See <see cref="ToApi(bool)"/>
		/// </summary>
		/// <param name="recursive">If we should recurse on <see cref="CreatedBy"/></param>
		/// <param name="showDetails">If rights and system identifier should be shown</param>
		/// <returns>A new <see cref="Api.Models.User"/></returns>
		Api.Models.User ToApi(bool recursive, bool showDetails) => new Api.Models.User
		{
			AdministrationRights = showDetails ? AdministrationRights : null,
			CreatedAt = CreatedAt,
			CreatedBy = recursive ? CreatedBy?.ToApi(false, false) : null,
			Enabled = Enabled,
			Id = Id,
			InstanceManagerRights = showDetails ? InstanceManagerRights : null,
			Name = Name,
			SystemIdentifier = showDetails ? SystemIdentifier : null,
			OAuthConnections = OAuthConnections
				?.Select(x => x.ToApi())
				.ToList()
				?? new List<Api.Models.OAuthConnection>(),
		};

		/// <summary>
		/// Convert the <see cref="User"/> to it's API form
		/// </summary>
		/// <param name="showDetails">If rights and system identifier should be shown</param>
		/// <returns>A new <see cref="Api.Models.User"/></returns>
		public Api.Models.User ToApi(bool showDetails) => ToApi(true, showDetails);
	}
}
