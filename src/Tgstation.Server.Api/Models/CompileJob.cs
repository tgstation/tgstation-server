using System;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class CompileJob : Internal.CompileJob
	{
		/// <summary>
		/// The <see cref="Job"/> relating to this job
		/// </summary>
		public Job? Job { get; set; }

		/// <summary>
		/// Git revision the compiler ran on. Not modifiable
		/// </summary>
		public RevisionInformation? RevisionInformation { get; set; }

		/// <summary>
		/// The <see cref="Byond.Version"/> the <see cref="CompileJob"/> was made with
		/// </summary>
		public Version? ByondVersion { get; set; }

		/// <summary>
		/// The origin <see cref="Uri"/> of the repository the compile job was built from.
		/// </summary>
		public Uri? RepositoryOrigin { get; set; }
	}
}
