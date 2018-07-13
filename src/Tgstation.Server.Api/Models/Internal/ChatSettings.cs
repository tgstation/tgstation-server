using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Manage the server chat bots
	/// </summary>
	[Model(RightsType.ChatSettings, RequiresInstance = true, CanCrud = true, ReadRight = ChatSettingsRights.Read)]
	public class ChatSettings
	{
		/// <summary>
		/// The settings id
		/// </summary>
		[Permissions(DenyWrite = true)]
		public long Id { get; set; }

		/// <summary>
		/// The name of the connection
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.WriteName)]
		[Required]
		public string Name { get; set; }

		/// <summary>
		/// If the connection is enabled
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.WriteEnabled)]
		public bool Enabled { get; set; }

		/// <summary>
		/// The <see cref="ChatProvider"/> used for the connection
		/// </summary>
		[Permissions(WriteRight = ChatSettingsRights.WriteProvider)]
		public ChatProvider Provider { get; set; }

		/// <summary>
		/// The information used to connect to the <see cref="Provider"/>
		/// </summary>
		[Permissions(ReadRight = ChatSettingsRights.ReadConnectionString, WriteRight = ChatSettingsRights.ReadConnectionString)]
		[Required]
		public string ConnectionString { get; set; }
	}
}
