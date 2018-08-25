namespace Tgstation.Server.Host.Components.Interop
{
	static class Constants
	{
		//interop values, match them up with the appropriate api.dm

		//api version 4.0.0.0
		public const string DMParamHostVersion = "server_service_version";
		public const string DMParamInfoJson = "tgs_json";

		public const string DMInteropAccessIdentifier = "tgs_tok";

		public const string DMResponseSuccess = "tgs_succ";

		public const string DMTopicChangePort = "tgs_port";
		public const string DMTopicChangeReboot = "tgs_rmode";
		public const string DMTopicChatCommand = "tgs_chat_comm";
		public const string DMTopicEvent = "tgs_event";
		public const string DMTopicInteropResponse = "tgs_interop";

		public const string DMCommandNewPort = "tgs_new_port";
		public const string DMCommandApiValidate = "tgs_validate";
		public const string DMCommandServerPrimed = "tgs_prime";
		public const string DMCommandWorldReboot = "tgs_reboot";
		public const string DMCommandEndProcess = "tgs_kill";
		public const string DMCommandChat = "tgs_chat_send";

		public const string DMParameterCommand = "tgs_com";
		public const string DMParameterData = "tgs_data";
	}
}
