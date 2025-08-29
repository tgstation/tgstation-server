using System;

using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc cref="Api.Models.Internal.CompileJob" />
	public sealed class CompileJob : Api.Models.Internal.CompileJob, ILegacyApiTransformable<CompileJobResponse>
	{
		/// <summary>
		/// See <see cref="CompileJobResponse.Job"/>.
		/// </summary>
		public required Job Job { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="Job"/>.
		/// </summary>
		public long JobId { get; set; } // Needed to determine the dependent side of the FK relationship

		/// <summary>
		/// See <see cref="CompileJobResponse.RevisionInformation"/>.
		/// </summary>
		public required RevisionInformation RevisionInformation { get; set; }

		/// <summary>
		/// The <see cref="Version"/> the <see cref="CompileJob"/> was made with in string form.
		/// </summary>
		public required string EngineVersion { get; set; }

		/// <summary>
		/// Backing field for <see cref="Version.Major"/> of <see cref="DMApiVersion"/>.
		/// </summary>
		public int? DMApiMajorVersion { get; set; }

		/// <summary>
		/// Backing field for <see cref="Version.Minor"/> of <see cref="DMApiVersion"/>.
		/// </summary>
		public int? DMApiMinorVersion { get; set; }

		/// <summary>
		/// Backing field for <see cref="Version.Build"/> of <see cref="DMApiVersion"/>.
		/// </summary>
		public int? DMApiPatchVersion { get; set; }

		/// <summary>
		/// The origin <see cref="Uri"/> of the repository the compile job was built from.
		/// </summary>
		public string? RepositoryOrigin { get; set; }

		/// <summary>
		/// The source GitHub repository the deployment came from if any.
		/// </summary>
		public long? GitHubRepoId { get; set; }

		/// <summary>
		/// The GitHub deployment ID associated with the <see cref="CompileJob"/> if any.
		/// </summary>
		public long? GitHubDeploymentId { get; set; }

		/// <inheritdoc />
		public override Version? DMApiVersion
		{
			get
			{
				if (!DMApiMajorVersion.HasValue)
					return null;

				return new Version(DMApiMajorVersion.Value, DMApiMinorVersion!.Value, DMApiPatchVersion!.Value);
			}

			set
			{
				DMApiMajorVersion = value?.Major;
				DMApiMinorVersion = value?.Minor;
				DMApiPatchVersion = value?.Build;
			}
		}

		/// <inheritdoc />
		public CompileJobResponse ToApi() => new()
		{
			DirectoryName = DirectoryName,
			DmeName = DmeName,
			Id = Id,
			Job = Job.ToApi(),
			Output = Output,
			RevisionInformation = RevisionInformation.ToApi(),
			EngineVersion = Api.Models.EngineVersion.TryParse(EngineVersion, out var version)
				? version
				: throw new InvalidOperationException($"Failed to parse engine version: {EngineVersion}"),
			MinimumSecurityLevel = MinimumSecurityLevel,
			DMApiVersion = DMApiVersion,
			RepositoryOrigin = RepositoryOrigin != null ? new Uri(RepositoryOrigin) : null,
		};
	}
}
