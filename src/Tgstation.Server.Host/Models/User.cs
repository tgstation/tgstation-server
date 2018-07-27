using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class User : Api.Models.Internal.User, IApiConvertable<Api.Models.User>
	{
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
		public string CanonicalName { get; set; }

		/// <summary>
		/// <see cref="User"/>s created by this <see cref="User"/>
		/// </summary>
		public List<User> CreatedUsers { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s for the <see cref="User"/>
		/// </summary>
		public List<InstanceUser> InstanceUsers { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s made by the <see cref="User"/>
		/// </summary>
		public List<TestMerge> TestMerges { get; set; }

		/// <summary>
		/// See <see cref="ToApi()"/>
		/// </summary>
		/// <param name="recursive">If we should recurse on <see cref="CreatedBy"/></param>
		/// <returns>A new <see cref="Api.Models.User"/></returns>
		Api.Models.User ToApi(bool recursive) => new Api.Models.User
		{
			AdministrationRights = AdministrationRights,
			CreatedAt = CreatedAt,
			CreatedBy = recursive ? CreatedBy?.ToApi(false) : null,
			Enabled = Enabled,
			Id = Id,
			InstanceManagerRights = InstanceManagerRights,
			Name = Name,
			SystemIdentifier = SystemIdentifier
		};

		/// <inheritdoc />
		public Api.Models.User ToApi() => ToApi(true);
	}
}
