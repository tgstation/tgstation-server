using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class DreamMakerSettings : Api.Models.DreamMaker, IApiTransformable<Api.Models.DreamMaker>
	{
		/// <summary>
		/// The row Id
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The instance <see cref="Api.Models.EntityId.Id"/>
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// The parent <see cref="Models.Instance"/>
		/// </summary>
		[Required]
		public Instance Instance { get; set; }

		/// <inheritdoc />
		public Api.Models.DreamMaker ToApi() => new Api.Models.DreamMaker
		{
			ProjectName = ProjectName,
			ApiValidationPort = ApiValidationPort,
			ApiValidationSecurityLevel = ApiValidationSecurityLevel,
			RequireDMApiValidation = RequireDMApiValidation
		};
	}
}
