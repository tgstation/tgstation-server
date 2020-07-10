using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents an <see cref="Api.Models.Instance"/> in the database
	/// </summary>
	public sealed class Instance : Api.Models.Instance
	{
		/// <summary>
		/// Default for <see cref="Api.Models.Instance.ChatBotLimit"/>.
		/// </summary>
		public const ushort DefaultChatBotLimit = 10;

		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> for the <see cref="Instance"/>
		/// </summary>
		public DreamMakerSettings DreamMakerSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> for the <see cref="Instance"/>
		/// </summary>
		public DreamDaemonSettings DreamDaemonSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> for the <see cref="Instance"/>
		/// </summary>
		public RepositorySettings RepositorySettings { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s in the <see cref="Instance"/>
		/// </summary>
		public List<InstanceUser> InstanceUsers { get; set; }

		/// <summary>
		/// The <see cref="ChatBot"/>s for the <see cref="Instance"/>
		/// </summary>
		public List<ChatBot> ChatSettings { get; set; }

		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="Instance"/>
		/// </summary>
		public List<RevisionInformation> RevisionInformations { get; set; }

		/// <summary>
		/// The <see cref="Job"/>s in the <see cref="Instance"/>
		/// </summary>
		public List<Job> Jobs { get; set; }

		/// <summary>
		/// Convert the <see cref="Instance"/> to it's API form
		/// </summary>
		/// <returns>A new <see cref="Api.Models.Instance"/></returns>
		public Api.Models.Instance ToApi() => new Api.Models.Instance
		{
			AutoUpdateInterval = AutoUpdateInterval,
			ConfigurationType = ConfigurationType,
			Id = Id,
			Name = Name,
			Path = Path,
			Online = Online,
			ChatBotLimit = ChatBotLimit
		};
	}
}
