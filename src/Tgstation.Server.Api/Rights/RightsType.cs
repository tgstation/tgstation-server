namespace Tgstation.Server.Api.Rights
{
	/// <summary>
	/// The type of rights a model uses
	/// </summary>
	public enum RightsType : ulong
	{
		/// <summary>
		/// <see cref="AdministrationRights"/>
		/// </summary>
		Administration,

		/// <summary>
		/// <see cref="InstanceManagerRights"/>
		/// </summary>
		InstanceManager,

		/// <summary>
		/// <see cref="RepositoryRights"/>
		/// </summary>
		Repository,

		/// <summary>
		/// <see cref="ByondRights"/>
		/// </summary>
		Byond,

		/// <summary>
		/// <see cref="DreamMakerRights"/>
		/// </summary>
		DreamMaker,

		/// <summary>
		/// <see cref="DreamDaemonRights"/>
		/// </summary>
		DreamDaemon,

		/// <summary>
		/// <see cref="ChatBotRights"/>
		/// </summary>
		ChatBots,

		/// <summary>
		/// <see cref="ConfigurationRights"/>
		/// </summary>
		Configuration,

		/// <summary>
		/// <see cref="InstancePermissionSetRights"/>
		/// </summary>
		InstancePermissionSet
	}
}
