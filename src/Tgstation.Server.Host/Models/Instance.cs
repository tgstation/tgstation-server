using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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
		/// <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		[NotMapped]
		public new long Id
		{
			get => base.Id ?? throw new InvalidOperationException("Id was null!");
			set => base.Id = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Instance.ChatBotLimit"/>.
		/// </summary>
		[NotMapped]
		public new ushort ChatBotLimit
		{
			get => base.ChatBotLimit ?? throw new InvalidOperationException("ChatBotLimit was null!");
			set => base.ChatBotLimit = value;
		}

		/// <summary>
		/// <see cref="Api.Models.NamedEntity.Name"/>.
		/// </summary>
		[NotMapped]
		public new string Name
		{
			get => base.Name ?? throw new InvalidOperationException("Name was null!");
			set => base.Name = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Instance.Path"/>.
		/// </summary>
		[NotMapped]
		public new string Path
		{
			get => base.Path ?? throw new InvalidOperationException("Path was null!");
			set => base.Path = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Instance.Online"/>.
		/// </summary>
		[NotMapped]
		public new bool Online
		{
			get => base.Online ?? throw new InvalidOperationException("Online was null!");
			set => base.Online = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Instance.AutoUpdateInterval"/>.
		/// </summary>
		[NotMapped]
		public new uint AutoUpdateInterval
		{
			get => base.AutoUpdateInterval ?? throw new InvalidOperationException("AutoUpdateInterval was null!");
			set => base.AutoUpdateInterval = value;
		}

		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> for the <see cref="Instance"/>.
		/// </summary>
		public DreamMakerSettings DreamMakerSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> for the <see cref="Instance"/>.
		/// </summary>
		public DreamDaemonSettings DreamDaemonSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> for the <see cref="Instance"/>.
		/// </summary>
		public RepositorySettings RepositorySettings { get; set; }

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

		/// <inheritdoc />
		public InstanceResponse ToApi() => new ()
		{
			AutoUpdateInterval = AutoUpdateInterval,
			ConfigurationType = ConfigurationType,
			Id = Id,
			Name = Name,
			Path = Path,
			Online = Online,
			ChatBotLimit = ChatBotLimit,
		};
	}
}
