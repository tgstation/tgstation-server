using System.Collections.Generic;

using Octokit;

namespace Tgstation.Server.ReleaseNotes
{
	sealed class Change
	{
		public List<string> Descriptions { get; set; }
		public string Author { get; set; }
		public int PullRequest { get; set; }
	}
}
