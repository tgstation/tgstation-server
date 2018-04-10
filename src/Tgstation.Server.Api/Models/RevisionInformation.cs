using System.Collections.Generic;

namespace Tgstation.Server.Api.Models
{
	public sealed class RevisionInformation : Internal.RevisionInformation
	{		
		public List<TestMerge> TestMerges { get; set; }
	}
}
