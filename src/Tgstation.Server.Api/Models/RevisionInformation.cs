using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models
{
	public sealed class RevisionInformation
	{
		[Key]
		string Revision { get; set; }

		[Required]
		string OriginRevision { get; set; }

		List<TestMerge> TestMerges { get; set; }
	}
}
