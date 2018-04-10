using System.ComponentModel.DataAnnotations;

namespace Tgstation.Server.Api.Models.Internal
{
	public class RevisionInformation
	{
		[Required, StringLength(40)]
		public string Revision { get; set; }

		[Required, StringLength(40)]
		public string OriginRevision { get; set; }
	}
}
