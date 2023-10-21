using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Tgstation.Server.Api.Models.Response;
using Tgstation.Server.Api.Rights;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.InstancePermissionSet" />
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

		/// <inheritdoc />
		[Column("ByondRights")]
		public override EngineRights? EngineRights
		{
			get => base.EngineRights;
			set => base.EngineRights = value;
		}

		/// <inheritdoc />
		public InstancePermissionSetResponse ToApi() => new InstancePermissionSetResponse
		{
#pragma warning disable CS0618 // Type or member is obsolete
			ByondRights = EngineRights,
#pragma warning restore CS0618 // Type or member is obsolete
			EngineRights = EngineRights,
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
