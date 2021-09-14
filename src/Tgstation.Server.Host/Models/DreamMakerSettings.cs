using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class DreamMakerSettings : Api.Models.Internal.DreamMakerSettings, IApiTransformable<DreamMakerResponse>
	{
		/// <summary>
		/// The row Id.
		/// </summary>
		public long Id { get; set; }

		/// <summary>
		/// The instance <see cref="EntityId.Id"/>.
		/// </summary>
		public long InstanceId { get; set; }

		/// <summary>
		/// <see cref="Api.Models.Internal.DreamMakerSettings.ApiValidationPort"/>.
		/// </summary>
		[NotMapped]
		public new ushort ApiValidationPort
		{
			get => base.ApiValidationPort ?? throw new InvalidOperationException("ApiValidationPort was null!");
			set => base.ApiValidationPort = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.DreamMakerSettings.Timeout"/>.
		/// </summary>
		[NotMapped]
		public new TimeSpan Timeout
		{
			get => base.Timeout ?? throw new InvalidOperationException("Timeout was null!");
			set => base.Timeout = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.DreamMakerSettings.RequireDMApiValidation"/>.
		/// </summary>
		[NotMapped]
		public new bool RequireDMApiValidation
		{
			get => base.RequireDMApiValidation ?? throw new InvalidOperationException("RequireDMApiValidation was null!");
			set => base.RequireDMApiValidation = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.DreamMakerSettings.ApiValidationSecurityLevel"/>.
		/// </summary>
		[NotMapped]
		public new DreamDaemonSecurity ApiValidationSecurityLevel
		{
			get => base.ApiValidationSecurityLevel ?? throw new InvalidOperationException("ApiValidationSecurityLevel was null!");
			set => base.ApiValidationSecurityLevel = value;
		}

		/// <summary>
		/// The parent <see cref="Models.Instance"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(instance))]
		public Instance Instance
		{
			get => instance ?? throw new InvalidOperationException("Instance not set!");
			set => instance = value;
		}

		/// <summary>
		/// Backing field for <see cref="Instance"/>.
		/// </summary>
		Instance? instance;

		/// <inheritdoc />
		public DreamMakerResponse ToApi() => new ()
		{
			ProjectName = ProjectName,
			ApiValidationPort = ApiValidationPort,
			ApiValidationSecurityLevel = ApiValidationSecurityLevel,
			RequireDMApiValidation = RequireDMApiValidation,
			Timeout = Timeout,
		};
	}
}
