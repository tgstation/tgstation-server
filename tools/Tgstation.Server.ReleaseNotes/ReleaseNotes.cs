using System.Collections.Generic;

namespace Tgstation.Server.ReleaseNotes
{
	sealed class ReleaseNotes
	{
		public SortedDictionary<Component, List<Changelist>> Components { get; set; }
	}
}
