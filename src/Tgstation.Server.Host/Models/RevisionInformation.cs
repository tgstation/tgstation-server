using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	sealed class RevisionInformation : Api.Models.Internal.RevisionInformation
	{
		public long Id { get; set; }
		
		public List<TestMerge> TestMerges { get; set; }
	}
}
