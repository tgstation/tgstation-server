using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class InstancePermissionSet : Api.Models.Internal.InstancePermissionSet, IApiTransformable<InstancePermissionSetResponse>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="Instance"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The <see cref="Models.Instance"/> the <see cref="InstancePermissionSet"/> belongs to.
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <summary>
		/// The <see cref="Models.PermissionSet"/> the <see cref="InstancePermissionSet"/> belongs to.
		/// </summary>
		[Required]
		public PermissionSet PermissionSet { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.Internal.InstancePermissionSet.RepositoryRights"/>.
		/// </summary>
		[NotMapped]
		public new RepositoryRights RepositoryRights
		{
			get => base.RepositoryRights ?? throw new InvalidOperationException("RepositoryRights was null!");
			set => base.RepositoryRights = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.InstancePermissionSet.ByondRights"/>.
		/// </summary>
		[NotMapped]
		public new ByondRights ByondRights
		{
			get => base.ByondRights ?? throw new InvalidOperationException("ByondRights was null!");
			set => base.ByondRights = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.InstancePermissionSet.DreamMakerRights"/>.
		/// </summary>
		[NotMapped]
		public new DreamMakerRights DreamMakerRights
		{
			get => base.DreamMakerRights ?? throw new InvalidOperationException("DreamMakerRights was null!");
			set => base.DreamMakerRights = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.InstancePermissionSet.DreamDaemonRights"/>.
		/// </summary>
		[NotMapped]
		public new DreamDaemonRights DreamDaemonRights
		{
			get => base.DreamDaemonRights ?? throw new InvalidOperationException("DreamDaemonRights was null!");
			set => base.DreamDaemonRights = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.InstancePermissionSet.ChatBotRights"/>.
		/// </summary>
		[NotMapped]
		public new ChatBotRights ChatBotRights
		{
			get => base.ChatBotRights ?? throw new InvalidOperationException("ChatBotRights was null!");
			set => base.ChatBotRights = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.InstancePermissionSet.ConfigurationRights"/>.
		/// </summary>
		[NotMapped]
		public new ConfigurationRights ConfigurationRights
		{
			get => base.ConfigurationRights ?? throw new InvalidOperationException("ConfigurationRights was null!");
			set => base.ConfigurationRights = value;
		}

		/// <summary>
		/// See <see cref="Api.Models.Internal.InstancePermissionSet.InstancePermissionSetRights"/>.
		/// </summary>
		[NotMapped]
		public new InstancePermissionSetRights InstancePermissionSetRights
		{
			get => base.InstancePermissionSetRights ?? throw new InvalidOperationException("InstancePermissionSetRights was null!");
			set => base.InstancePermissionSetRights = value;
		}

		/// <inheritdoc />
		public InstancePermissionSetResponse ToApi() => new ()
		{
			ByondRights = ByondRights,
			ChatBotRights = ChatBotRights,
			ConfigurationRights = ConfigurationRights,
			DreamDaemonRights = DreamDaemonRights,
			DreamMakerRights = DreamMakerRights,
			RepositoryRights = RepositoryRights,
			InstancePermissionSetRights = InstancePermissionSetRights,
			PermissionSetId = PermissionSetId,
		};
	}
}
