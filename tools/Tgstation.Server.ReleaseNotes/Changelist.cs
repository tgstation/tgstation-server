using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Tgstation.Server.ReleaseNotes
{
	sealed class Changelist
	{
		public Version Version { get; set; }

		public Dictionary<Component, Version> ComponentVersions { get; set; }

		public List<Change> Changes { get; set; }

		public bool Unreleased { get; set; }

		public void DeduplicateChanges()
		{
			Changes = Changes
				.OrderBy(x => x.PullRequest)
				.GroupBy(x => x.PullRequest)
				.Select(prChanges =>
				{
					string author = null;
					return new Change
					{
						PullRequest = prChanges.Key,
						Descriptions = prChanges
							.SelectMany(x =>
							{
								if (author != null)
									Debug.Assert(x.Author == author);
								else
									author = x.Author;

								return x.Descriptions;
							})
							.ToList(),
						Author = author
					};
				})
				.ToList();
		}
	}
}
