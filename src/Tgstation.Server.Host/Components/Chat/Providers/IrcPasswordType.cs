namespace Tgstation.Server.Host.Components.Chat.Providers
{
	/// <summary>
	/// Represents the type of a password passed to the constructor of <see cref="IrcProvider"/>
	/// </summary>
	enum IrcPasswordType
	{
		/// <summary>
		/// Use server authentication
		/// </summary>
		Server,
		/// <summary>
		/// Use PLAIN sasl authentication
		/// </summary>
		Sasl,
		/// <summary>
		/// Use NickServ authentication
		/// </summary>
		NickServ
	}
}
