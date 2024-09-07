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
		public Instance? Instance { get; set; }

		/// <inheritdoc />
		public DreamMakerResponse ToApi() => new()
		{
			ProjectName = ProjectName,
			ApiValidationPort = ApiValidationPort,
			ApiValidationSecurityLevel = ApiValidationSecurityLevel,
#pragma warning disable CS0618 // Type or member is obsolete
			RequireDMApiValidation = DMApiValidationMode == Api.Models.DMApiValidationMode.Required,
#pragma warning restore CS0618 // Type or member is obsolete
			DMApiValidationMode = DMApiValidationMode,
			Timeout = Timeout,
			CompilerAdditionalArguments = CompilerAdditionalArguments,
		};
	}
}
