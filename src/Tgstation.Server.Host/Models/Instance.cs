using System.Collections.Generic;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents an <see cref="Api.Models.Instance"/> in the database.
	/// </summary>
	public sealed class Instance : Api.Models.Instance, IApiTransformable<InstanceResponse>
	{
		/// <summary>
		/// Default for <see cref="Api.Models.Instance.ChatBotLimit"/>.
		/// </summary>
		public const ushort DefaultChatBotLimit = 10;

		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> for the <see cref="Instance"/>.
		/// </summary>
		public DreamMakerSettings? DreamMakerSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> for the <see cref="Instance"/>.
		/// </summary>
		public DreamDaemonSettings? DreamDaemonSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> for the <see cref="Instance"/>.
		/// </summary>
		public RepositorySettings? RepositorySettings { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.Internal.SwarmServer.Identifier"/> of the the server in the swarm this instance belongs to.
		/// </summary>
		public string? SwarmIdentifer { get; set; }

		/// <summary>
		/// The <see cref="InstancePermissionSet"/>s in the <see cref="Instance"/>.
		/// </summary>
		public ICollection<InstancePermissionSet> InstancePermissionSets { get; set; }

		/// <summary>
		/// The <see cref="ChatBot"/>s for the <see cref="Instance"/>.
		/// </summary>
		public ICollection<ChatBot> ChatSettings { get; set; }

		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="Instance"/>.
		/// </summary>
		public ICollection<RevisionInformation> RevisionInformations { get; set; }

		/// <summary>
		/// The <see cref="Job"/>s in the <see cref="Instance"/>.
		/// </summary>
		public ICollection<Job> Jobs { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Instance"/> class.
		/// </summary>
		public Instance()
		{
			InstancePermissionSets = new List<InstancePermissionSet>();
			ChatSettings = new List<ChatBot>();
			RevisionInformations = new List<RevisionInformation>();
			Jobs = new List<Job>();
		}

		/// <inheritdoc />
		public InstanceResponse ToApi() => new()
		{
			AutoUpdateInterval = AutoUpdateInterval,
			AutoUpdateCron = AutoUpdateCron,
			ConfigurationType = ConfigurationType,
			Id = Id,
			Name = Name,
			Path = Path,
			Online = Online,
			ChatBotLimit = ChatBotLimit,
		};
	}
}
