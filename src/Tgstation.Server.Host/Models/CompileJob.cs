using System;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <inheritdoc />
	public sealed class CompileJob : Api.Models.Internal.CompileJob
	{
		/// <summary>
		/// See <see cref="Api.Models.CompileJob.Job"/>
		/// </summary>
		[Required]
		public Job Job { get; set; }

		/// <summary>
		/// The <see cref="Api.Models.EntityId.Id"/> of <see cref="Job"/>
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
		/// Convert the <see cref="CompileJob"/> to it's API form
		/// </summary>
		/// <returns>A new <see cref="Api.Models.CompileJob"/></returns>
		public Api.Models.CompileJob ToApi() => new Api.Models.CompileJob
		{
			DirectoryName = DirectoryName,
			DmeName = DmeName,
			Id = Id,
			Job = Job.ToApi(),
			Output = Output,
			RevisionInformation = RevisionInformation.ToApi(),
			ByondVersion = Version.Parse(ByondVersion),
			MinimumSecurityLevel = MinimumSecurityLevel
		};
	}
}
