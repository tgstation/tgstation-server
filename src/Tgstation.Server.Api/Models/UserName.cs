namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Base class for user names.
	/// </summary>
	public class UserName : NamedEntity
	{
		/// <inheritdoc />
		/// <example>Admin</example>
		[RequestOptions(FieldPresence.Optional)]
		public override string? Name
		{
			get => base.Name;
			set => base.Name = value;
		}

		/// <summary>
		/// Create a copy of the <see cref="UserName"/>.
		/// </summary>
		/// <returns>A new <see cref="UserName"/> copied from <see langword="this"/>.</returns>
		public UserName CreateUserName() => CreateUserName<UserName>();

		/// <summary>
		/// Create a copy of the <see cref="UserName"/> as a given <typeparamref name="TResultType"/>.
		/// </summary>
		/// <typeparam name="TResultType">The child of <see cref="UserName"/> to create.</typeparam>
		/// <returns>A new <typeparamref name="TResultType"/> copied from <see langword="this"/>.</returns>
		protected virtual TResultType CreateUserName<TResultType>()
			where TResultType : UserName, new() => new()
		{
			Id = Id,
			Name = Name,
		};
	}
}
