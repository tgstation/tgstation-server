using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

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
		[Required]
		[BackingField(nameof(dreamMakerSettings))]
		public DreamMakerSettings DreamMakerSettings
		{
			get => dreamMakerSettings ?? throw new InvalidOperationException("DreamMakerSettings not set!");
			set => dreamMakerSettings = value;
		}

		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> for the <see cref="Instance"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(dreamDaemonSettings))]
		public DreamDaemonSettings DreamDaemonSettings
		{
			get => dreamDaemonSettings ?? throw new InvalidOperationException("DreamDaemonSettings not set!");
			set => dreamDaemonSettings = value;
		}

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> for the <see cref="Instance"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(repositorySettings))]
		public RepositorySettings RepositorySettings
		{
			get => repositorySettings ?? throw new InvalidOperationException("RepositorySettings not set!");
			set => repositorySettings = value;
		}

		/// <summary>
		/// The <see cref="Api.Models.Internal.SwarmServer.Identifier"/> of the the server in the swarm this instance belongs to.
		/// </summary>
		public string? SwarmIdentifer { get; set; }

		/// <summary>
		/// The <see cref="InstancePermissionSet"/>s in the <see cref="Instance"/>.
		/// </summary>
		[BackingField(nameof(instancePermissionSets))]
		public ICollection<InstancePermissionSet> InstancePermissionSets
		{
			get => instancePermissionSets ?? throw new InvalidOperationException("InstancePermissionSets not set!");
			set => instancePermissionSets = value;
		}

		/// <summary>
		/// The <see cref="ChatBot"/>s for the <see cref="Instance"/>.
		/// </summary>
		[BackingField(nameof(chatSettings))]
		public ICollection<ChatBot> ChatSettings
		{
			get => chatSettings ?? throw new InvalidOperationException("ChatSettings not set!");
			set => chatSettings = value;
		}

		/// <summary>
		/// The <see cref="RevisionInformation"/>s in the <see cref="Instance"/>.
		/// </summary>
		[BackingField(nameof(revisionInformations))]
		public ICollection<RevisionInformation> RevisionInformations
		{
			get => revisionInformations ?? throw new InvalidOperationException("RevisionInformations not set!");
			set => revisionInformations = value;
		}

		/// <summary>
		/// The <see cref="Job"/>s in the <see cref="Instance"/>.
		/// </summary>
		[BackingField(nameof(jobs))]
		public ICollection<Job> Jobs
		{
			get => jobs ?? throw new InvalidOperationException("Jobs not set!");
			set => jobs = value;
		}

		/// <summary>
		/// Backing field for <see cref="InstancePermissionSets"/>.
		/// </summary>
		ICollection<InstancePermissionSet>? instancePermissionSets;

		/// <summary>
		/// Backing field for <see cref="ChatSettings"/>.
		/// </summary>
		ICollection<ChatBot>? chatSettings;

		/// <summary>
		/// Backing field for <see cref="RevisionInformations"/>.
		/// </summary>
		ICollection<RevisionInformation>? revisionInformations;

		/// <summary>
		/// Backing field for <see cref="Jobs"/>.
		/// </summary>
		ICollection<Job>? jobs;

		/// <summary>
		/// Backing field for <see cref="DreamMakerSettings"/>.
		/// </summary>
		DreamMakerSettings? dreamMakerSettings;

		/// <summary>
		/// Backing field for <see cref="DreamDaemonSettings"/>.
		/// </summary>
		DreamDaemonSettings? dreamDaemonSettings;

		/// <summary>
		/// Backing field for <see cref="RepositorySettings"/>.
		/// </summary>
		RepositorySettings? repositorySettings;

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
