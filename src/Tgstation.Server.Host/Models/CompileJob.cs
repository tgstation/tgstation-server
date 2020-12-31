using System;
using System.ComponentModel.DataAnnotations;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class CompileJob : Api.Models.Internal.CompileJob, IApiTransformable<Api.Models.CompileJob>
	{
		/// <summary>
		/// See <see cref="Api.Models.CompileJob.Job"/>
		/// </summary>
		[Required]
		public Job Job { get; set; }

		/// <summary>
		/// The <see cref="EntityId.Id"/> of <see cref="Job"/>
		/// </summary>
		public long JobId { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.CompileJob.RevisionInformation"/>
		/// </summary>
		[Required]
		public RevisionInformation RevisionInformation { get; set; }

		/// <summary>
		/// The <see cref="Version"/> the <see cref="CompileJob"/> was made with in string form
		/// </summary>
		[Required]
		public string ByondVersion { get; set; }

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
		public string RepositoryOrigin { get; set; }

		/// <summary>
		/// The source GitHub repository the deployment came from if any.
		/// </summary>
		public long? GitHubRepoId { get; set; }

		/// <summary>
		/// The GitHub deployment ID associated with the <see cref="CompileJob"/> if any.
		/// </summary>
		public int? GitHubDeploymentId { get; set; }

		/// <inheritdoc />
		public override Version DMApiVersion
		{
			get
			{
				if (!DMApiMajorVersion.HasValue)
					return null;

				return new Version(DMApiMajorVersion.Value, DMApiMinorVersion.Value, DMApiPatchVersion.Value);
			}
			set
			{
				DMApiMajorVersion = value?.Major;
				DMApiMinorVersion = value?.Minor;
				DMApiPatchVersion = value?.Build;
			}
		}

		/// <inheritdoc />
		public Api.Models.CompileJob ToApi() => new Api.Models.CompileJob
		{
			DirectoryName = DirectoryName,
			DmeName = DmeName,
			Id = Id,
			Job = Job.ToApi(),
			Output = Output,
			RevisionInformation = RevisionInformation.ToApi(),
			ByondVersion = Version.Parse(ByondVersion),
			MinimumSecurityLevel = MinimumSecurityLevel,
			DMApiVersion = DMApiVersion,
			RepositoryOrigin = new Uri(RepositoryOrigin),
		};
	}
}
