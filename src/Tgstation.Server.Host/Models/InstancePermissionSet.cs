using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.InstancePermissionSet" />
	public sealed class InstancePermissionSet : Api.Models.Internal.InstancePermissionSet, ILegacyApiTransformable<InstancePermissionSetResponse>
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
		public Instance Instance { get; set; } = null!; // recommended by EF

		/// <summary>
		/// The <see cref="Models.PermissionSet"/> the <see cref="InstancePermissionSet"/> belongs to.
		/// </summary>
		public PermissionSet PermissionSet { get; set; } = null!; // recommended by EF

		/// <inheritdoc />
		public InstancePermissionSetResponse ToApi() => new()
		{
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
