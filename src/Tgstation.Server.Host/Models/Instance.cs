using System.Collections.Generic;
using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Models
{
	/// <summary>
	/// Represents an <see cref="Api.Models.Instance"/> in the database
	/// </summary>
	sealed class Instance : Api.Models.Instance
	{
		/// <summary>
		/// The <see cref="ChatSettings"/> for the <see cref="Instance"/>
		/// </summary>
		ChatSettings ChatSettings { get; set; }
		
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
	}
}
