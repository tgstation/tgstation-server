using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	/// <inheritdoc />
	public sealed class RevisionInformation : Internal.RevisionInformation
	{	
		/// <summary>
		/// The <see cref="TestMerge"/>s active in the <see cref="RevisionInformation"/>
		/// </summary>
		public List<TestMerge> TestMerges { get; set; }
	}
}
