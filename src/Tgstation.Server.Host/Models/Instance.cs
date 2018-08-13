using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents an <see cref="Api.Models.Instance"/> in the database
	/// </summary>
	public sealed class Instance : Api.Models.Instance, IApiConvertable<Api.Models.Instance>
	{

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
		/// The <see cref="Models.WatchdogReattachInformation"/> for the <see cref="Instance"/>
		/// </summary>
		public WatchdogReattachInformation WatchdogReattachInformation { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s in the <see cref="Instance"/>
		/// </summary>
		public List<InstanceUser> InstanceUsers { get; set; }

		/// <summary>
		/// The <see cref="Models.ChatBot"/>s for the <see cref="Instance"/>
		/// </summary>
		public List<ChatBot> ChatSettings { get; set; }

		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="Instance"/>
		/// </summary>
		public List<RevisionInformation> RevisionInformations { get; set; }
		
		/// <summary>
		/// The <see cref="Jobs"/> in the <see cref="Instance"/>
		/// </summary>
		public List<Job> Jobs { get; set; }

		/// <inheritdoc />
		public Api.Models.Instance ToApi() => new Api.Models.Instance
		{
			AutoUpdateInterval = AutoUpdateInterval,
			ConfigurationType = ConfigurationType,
			Id = Id,
			Name = Name,
			Path = Path,
			Online = Online
		};
	}
}
