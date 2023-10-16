using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Newtonsoft.Json;

using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Api.Models.Internal
{
	/// <summary>
	/// Represents a <see cref="PermissionSet"/>s permissions in an <see cref="Instance"/>.
	/// </summary>
	public abstract class InstancePermissionSet
	{
		/// <summary>
		/// The <see cref="EntityId.Id"/> of the <see cref="PermissionSet"/> the <see cref="InstancePermissionSet"/> belongs to.
		/// </summary>
		[RequestOptions(FieldPresence.Required)]
		public long PermissionSetId { get; set; }

		/// <summary>
		/// The <see cref="Rights.InstancePermissionSetRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		[Required]
		public InstancePermissionSetRights? InstancePermissionSetRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.EngineRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		[NotMapped]
		[JsonIgnore]
		public EngineRights? EngineRights
		{
#pragma warning disable CS0618 // Type or member is obsolete
			get => ByondRights;
			set => ByondRights = value;
#pragma warning restore CS0618 // Type or member is obsolete
		}

		/// <summary>
		/// The legacy <see cref="Rights.EngineRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		[Required]
		[Obsolete("Use EngineRights instead")]
		public EngineRights? ByondRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.DreamDaemonRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		[Required]
		public DreamDaemonRights? DreamDaemonRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.DreamMakerRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		[Required]
		public DreamMakerRights? DreamMakerRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.RepositoryRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		[Required]
		public RepositoryRights? RepositoryRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.ChatBotRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		[Required]
		public ChatBotRights? ChatBotRights { get; set; }

		/// <summary>
		/// The <see cref="Rights.ConfigurationRights"/> of the <see cref="InstancePermissionSet"/>.
		/// </summary>
		[Required]
		public ConfigurationRights? ConfigurationRights { get; set; }
	}
}
