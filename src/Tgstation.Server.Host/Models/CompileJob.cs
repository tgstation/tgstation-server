using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using Microsoft.EntityFrameworkCore;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Response;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class CompileJob : Api.Models.Internal.CompileJob, IApiTransformable<CompileJobResponse>
	{
		/// <summary>
		/// <see cref="EntityId.Id"/>.
		/// </summary>
		[NotMapped]
		public new long Id
		{
			get => base.Id ?? throw new InvalidOperationException("Id was null!");
			set => base.Id = value;
		}

		/// <summary>
		/// <see cref="Api.Models.Internal.CompileJob.DirectoryName"/>.
		/// </summary>
		[NotMapped]
		public new Guid DirectoryName
		{
			get => base.DirectoryName ?? throw new InvalidOperationException("DirectoryName was null!");
			set => base.DirectoryName = value;
		}

		/// <summary>
		/// See <see cref="CompileJobResponse.Job"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(job))]
		public Job Job
		{
			get => job ?? throw new InvalidOperationException("Job not set!");
			set => job = value;
		}

		/// <summary>
		/// The <see cref="EntityId.Id"/> of <see cref="Job"/>.
		/// </summary>
		public long JobId { get; set; }

		/// <summary>
		/// See <see cref="CompileJobResponse.RevisionInformation"/>.
		/// </summary>
		[Required]
		[BackingField(nameof(revisionInformation))]
		public RevisionInformation RevisionInformation
		{
			get => revisionInformation ?? throw new InvalidOperationException("RevisionInformation not set!");
			set => revisionInformation = value;
		}

		/// <summary>
		/// The <see cref="Version"/> the <see cref="CompileJob"/> was made with in string form.
		/// </summary>
		[Required]
		[BackingField(nameof(byondVersion))]
		public string ByondVersion
		{
			get => byondVersion ?? throw new InvalidOperationException("ByondVersion not set!");
			set => byondVersion = value;
		}

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
		public int? GitHubDeploymentId { get; set; }

		/// <summary>
		/// Backing field for <see cref="Job"/>.
		/// </summary>
		Job? job;

		/// <summary>
		/// Backing field for <see cref="RevisionInformation"/>.
		/// </summary>
		RevisionInformation? revisionInformation;

		/// <summary>
		/// Backing field for <see cref="ByondVersion"/>.
		/// </summary>
		string? byondVersion;

		/// <inheritdoc />
		public override Version? DMApiVersion
		{
			get
			{
				if (!DMApiMajorVersion.HasValue)
					return null;

				if (!DMApiMinorVersion.HasValue)
					throw new InvalidOperationException("DMApiMinorVersion was null!");

				if (!DMApiPatchVersion.HasValue)
					throw new InvalidOperationException("DMApiPatchVersion was null!");

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
		public CompileJobResponse ToApi() => new ()
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
			RepositoryOrigin = RepositoryOrigin != null ? new Uri(RepositoryOrigin) : null,
		};
	}
}
