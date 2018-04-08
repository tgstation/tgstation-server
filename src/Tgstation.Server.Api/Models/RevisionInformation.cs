using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Tgstation.Server.Api.Models
{
	public class RevisionInformation
	{
		[Required, StringLength(40)]
		public string Revision { get; set; }

		[Required, StringLength(40)]
		public string OriginRevision { get; set; }
		
		public List<TestMerge> TestMerges { get; set; }
	}
}
