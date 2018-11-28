namespace Tgstation.Server.Host.Components.Interop
{
	/// <summary>
	/// Constants used for communication with the DMAPI
	/// </summary>
	static class Constants
	{
		/// <summary>
		/// Identifies a TGS execution. The server version
		/// </summary>
		public const string DMParamHostVersion = "server_service_version";

		/// <summary>
		/// Path to the <see cref="JsonFile"/>
		/// </summary>
		public const string DMParamInfoJson = "tgs_json";

		/// <summary>
		/// The <see cref="JsonFile.AccessIdentifier"/>
		/// </summary>
		public const string DMInteropAccessIdentifier = "tgs_tok";

		/// <summary>
		/// Generic OK response
		/// </summary>
		public const string DMResponseSuccess = "tgs_succ";

		/// <summary>
		/// Change port
		/// </summary>
		public const string DMTopicChangePort = "tgs_port";

		/// <summary>
		/// Change reboot mode
		/// </summary>
		public const string DMTopicChangeReboot = "tgs_rmode";

		/// <summary>
		/// Chat command
		/// </summary>
		public const string DMTopicChatCommand = "tgs_chat_comm";

		/// <summary>
		/// Notify of an <see cref="EventType"/>
		/// </summary>
		public const string DMTopicEvent = "tgs_event";

		/// <summary>
		/// Response to an interop export from DM
		/// </summary>
		public const string DMTopicInteropResponse = "tgs_interop";

		/// <summary>
		/// Set port command
		/// </summary>
		public const string DMCommandNewPort = "tgs_new_port";

		/// <summary>
		/// API validation command
		/// </summary>
		public const string DMCommandApiValidate = "tgs_validate";

		/// <summary>
		/// Server primed command
		/// </summary>
		public const string DMCommandServerPrimed = "tgs_prime";

		/// <summary>
		/// World reboot command
		/// </summary>
		public const string DMCommandWorldReboot = "tgs_reboot";

		/// <summary>
		/// Terminate process command
		/// </summary>
		public const string DMCommandEndProcess = "tgs_kill";

		/// <summary>
		/// Chat send command
		/// </summary>
		public const string DMCommandChat = "tgs_chat_send";

		/// <summary>
		/// Topic command parameter
		/// </summary>
		public const string DMParameterCommand = "tgs_com";

		/// <summary>
		/// Command data
		/// </summary>
		public const string DMParameterData = "tgs_data";
	}
}
