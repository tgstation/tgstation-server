using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents an <see cref="Api.Models.Instance"/> in the database
	/// </summary>
	sealed class Instance : Api.Models.Instance
	{
		/// <summary>
		/// The <see cref="Models.ChatSettings"/> for the <see cref="Instance"/>
		/// </summary>
		public ChatSettings ChatSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.DreamMakerSettings"/> for the <see cref="Instance"/>
		/// </summary>
		public DreamMakerSettings DreamMakerSettings { get; set; }
		
		/// <summary>
		/// The <see cref="Models.DreamDaemonSettings"/> for the <see cref="Instance"/>
		/// </summary>
		public DreamDaemonSettings DreamDaemonSettings { get; set; }

		/// <summary>
		/// The <see cref="Models.RepositorySettings"/> for the <see cref="Instance"/>
		/// </summary>
		public RepositorySettings RepositorySettings { get; set; }

		/// <summary>
		/// The <see cref="InstanceUser"/>s in the <see cref="Instance"/>
		/// </summary>
		public List<InstanceUser> InstanceUsers { get; set; }

		/// <summary>
		/// The <see cref="TestMerge"/>s in the <see cref="Instance"/>
		/// </summary>
		public List<TestMerge> TestMerges { get; set; }

		/// <summary>
		/// The <see cref="CompileJob"/>s in the <see cref="Instance"/>
		/// </summary>
		public List<CompileJob> CompileJobs { get; set; }

		/// <summary>
		/// The <see cref="Jobs"/> in the <see cref="Instance"/>
		/// </summary>
		public List<Job> Jobs { get; set; }
	}
}
