namespace Tgstation.Server.Api.Models
{
	/// <summary>
	/// Represents the type of a password for a <see cref="ChatProvider.Irc"/>.
	/// </summary>
	public enum IrcPasswordType
	{
		/// <summary>
		/// Use server authentication.
		/// </summary>
		Server,

		/// <summary>
		/// Use PLAIN sasl authentication.
		/// </summary>
		Sasl,

		/// <summary>
		/// Use NickServ authentication.
		/// </summary>
		NickServ,

		/// <summary>
		/// Use OPER authentication.
		/// </summary>
		Oper,
	}
}
