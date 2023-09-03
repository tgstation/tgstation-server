using System.ComponentModel.DataAnnotations;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.DreamMakerSettings" />
	public sealed class DreamMakerSettings : Api.Models.Internal.DreamMakerSettings, IApiTransformable<DreamMakerResponse>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The instance <see cref="Api.Models.EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>.
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <inheritdoc />
		public DreamMakerResponse ToApi() => new DreamMakerResponse
		{
			ProjectName = ProjectName,
			ApiValidationPort = ApiValidationPort,
			ApiValidationSecurityLevel = ApiValidationSecurityLevel,
			RequireDMApiValidation = RequireDMApiValidation,
			Timeout = Timeout,
		};
	}
}
