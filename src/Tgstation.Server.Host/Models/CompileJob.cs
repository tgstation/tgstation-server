using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class CompileJob : Api.Models.Internal.CompileJob, IApiConvertable<Api.Models.CompileJob>
	{
		/// <summary>
		/// The <see cref="Api.Models.Internal.Job.Id"/> of <see cref="Job"/>
		/// </summary>
		public long JobId { get; set; }

		/// <summary>
		/// See <see cref="Api.Models.CompileJob.Job"/>
		/// </summary>
		public Job Job { get; set; }

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

		/// <inheritdoc />
		public Api.Models.CompileJob ToApi() => new Api.Models.CompileJob
		{
			DirectoryName = DirectoryName,
			DMApiValidated = DMApiValidated,
			DmeName = DmeName,
			ExitCode = ExitCode,
			Id = Id,
			Job = Job.ToApi(),
			Output = Output,
			RevisionInformation = RevisionInformation.ToApi(),
			ByondVersion = Version.Parse(ByondVersion)
		};
	}
}
