namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Helper for building <see cref="ChatBot.ConnectionString"/>s
	/// </summary>
	public abstract class ChatConnectionStringBuilder
	{
		/// <summary>
		/// If the <see cref="ChatConnectionStringBuilder"/> evaluates to a valid <see cref="ChatBot.ConnectionString"/>
		/// </summary>
		public abstract bool Valid { get; }

		/// <summary>
		/// Gets the <see cref="ChatBot.ConnectionString"/> associated with the <see cref="ChatConnectionStringBuilder"/>
		/// </summary>
		/// <returns>The <see cref="ChatBot.ConnectionString"/> associated with the <see cref="ChatConnectionStringBuilder"/></returns>
		public abstract override string ToString();
	}
}