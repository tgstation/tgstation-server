using System.Collections.Generic;

namespace Tgstation.Server.Host.Models
{
	sealed class RevisionInformation : Api.Models.RevisionInformation
	{
		public long Id { get; set; }
		
		new public List<TestMerge> TestMerges { get; set; }
	}
}
