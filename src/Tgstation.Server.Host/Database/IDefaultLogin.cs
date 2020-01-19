namespace Tgstation.Server.Host.Database
{
	/// <summary>
	/// For setting the default login credentials upon server setup.
	/// </summary>
	interface IDefaultLogin
	{
		/// <summary>
		/// Gets the default username.
		/// </summary>
		string UserName { get; }

		/// <summary>
		/// Gets or sets the default password.
		/// </summary>
		string Password { get; set; }
	}
}