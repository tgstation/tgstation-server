using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Manage the server chat bots
	/// </summary>
	[Model(RightsType.ChatBots, RequiresInstance = true, CanList = true, CanCrud = true, ReadRight = ChatBotRights.Read)]
	public class ChatBot
	{
		/// <summary>
		/// The settings id
		/// </summary>
		[Permissions(DenyWrite = true)]
		public long Id { get; set; }

		/// <summary>
		/// The name of the connection
		/// </summary>
		[Permissions(WriteRight = ChatBotRights.WriteName)]
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// If the connection is enabled
		/// </summary>
		[Permissions(WriteRight = ChatBotRights.WriteEnabled)]
		public bool? Enabled { get; set; }

		/// <summary>
		/// The <see cref="ChatProvider"/> used for the connection
		/// </summary>
		[Permissions(WriteRight = ChatBotRights.WriteProvider)]
		public ChatProvider? Provider { get; set; }

		/// <summary>
		/// The information used to connect to the <see cref="Provider"/>
		/// </summary>
		[Permissions(ReadRight = ChatBotRights.ReadConnectionString, WriteRight = ChatBotRights.ReadConnectionString)]
		[Required]
		public string ConnectionString { get; set; }
	}
}
