using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ReleaseNotes
{
	/// <summary>
	/// Contains the application entrypoint
	/// </summary>
	static class Program
	{
		/// <summary>
		/// The entrypoint for the <see cref="Program"/>
		/// </summary>
		static async Task<int> Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Missing version argument!");
				return 1;
			}

			var versionString = args[0];
			if (!Version.TryParse(versionString, out var version) || version.Revision != -1)
			{
				Console.WriteLine("Invalid version: " + versionString);
				return 2;
			}

			var doNotCloseMilestone = args.Length > 1 && args[1].ToUpperInvariant() == "--NO-CLOSE";

			const string ReleaseNotesEnvVar = "TGS_RELEASE_NOTES_TOKEN";
			var githubToken = Environment.GetEnvironmentVariable(ReleaseNotesEnvVar);
			if (String.IsNullOrWhiteSpace(githubToken) && !doNotCloseMilestone)
			{
				Console.WriteLine("Missing " + ReleaseNotesEnvVar + " environment variable!");
				return 3;
			}

			try
			{
				var client = new GitHubClient(new ProductHeaderValue("tgs_release_notes"));
				if (!String.IsNullOrWhiteSpace(githubToken))
				{
					client.Credentials = new Credentials(githubToken);
				}

				const string RepoOwner = "tgstation";
				const string RepoName = "tgstation-server";

				var releasesTask = client.Repository.Release.GetAll(RepoOwner, RepoName);

				Console.WriteLine("Getting merged pull requests in milestone " + versionString + "...");
				var milestonePRs = await client.Search.SearchIssues(new SearchIssuesRequest
				{
					Milestone = $"v{versionString}",
					Type = IssueTypeQualifier.PullRequest,
					Repos = { { RepoOwner, RepoName } }
				}).ConfigureAwait(false);

				if (milestonePRs.IncompleteResults)
				{
					Console.WriteLine("Incomplete results for milestone PRs query!");
					return 5;
				}
				Console.WriteLine(milestonePRs.Items.Count + " total pull requests");

				Task<Milestone> milestoneTask = null;
				var milestoneTaskLock = new object();
				var releaseDictionary = new Dictionary<string, List<Tuple<string, int, string>>>(StringComparer.OrdinalIgnoreCase);
				var authorizedUsers = new Dictionary<long, Task<bool>>();

				bool postControlPanelMessage = false;

				async Task GetReleaseNotesFromPR(Issue pullRequest)
				{
					//need to check it was merged
					var fullPR = await client.Repository.PullRequest.Get(RepoOwner, RepoName, pullRequest.Number).ConfigureAwait(false);

					if (!fullPR.Merged)
					{
						if (!doNotCloseMilestone && fullPR.Milestone != null)
						{
							Console.WriteLine($"Removing trash PR #{fullPR.Number} from milestone...");
							await client.Issue.Update(RepoOwner, RepoName, fullPR.Number, new IssueUpdate
							{
								Milestone = null
							}).ConfigureAwait(false);
						}

						return;
					}

					async Task<Milestone> GetMilestone()
					{
						if (fullPR.Milestone == null)
							return null;
						return await client.Issue.Milestone.Get(RepoOwner, RepoName, fullPR.Milestone.Number);
					};

					lock (milestoneTaskLock)
						milestoneTask ??= GetMilestone();

					// if (!fullPR.Merged)
					//return;

					async Task BuildNotesFromComment(string comment, User user)
					{
						if (comment == null)
							return;

						async Task CommitNotes(string component, List<string> notes)
						{
							Task<bool> authTask;
							TaskCompletionSource<bool> ourTcs = null;
							lock (authorizedUsers)
							{
								if (!authorizedUsers.TryGetValue(user.Id, out authTask))
								{
									ourTcs = new TaskCompletionSource<bool>();
									authTask = ourTcs.Task;
									authorizedUsers.Add(user.Id, authTask);
								}
							}

							if (ourTcs != null)
								try
								{
									//check if the user has access
									var perm = String.IsNullOrWhiteSpace(githubToken)
										? PermissionLevel.Write
										: (await client.Repository.Collaborator.ReviewPermission(RepoOwner, RepoName, user.Login).ConfigureAwait(false)).Permission;
									ourTcs.SetResult(perm == PermissionLevel.Write || perm == PermissionLevel.Admin);
								}
								catch
								{
									ourTcs.SetResult(false);
									throw;
								}

							var authorized = await authTask.ConfigureAwait(false);
							if (!authorized)
								return;

							lock (releaseDictionary)
							{
								foreach (var I in notes)
									Console.WriteLine(component + " #" + fullPR.Number + " - " + I + " (@" + user.Login + ")");

								var tupleSelector = notes.Select(note => Tuple.Create(note, fullPR.Number, user.Login));
								if (releaseDictionary.TryGetValue(component, out var currentValues))
									currentValues.AddRange(tupleSelector);
								else
									releaseDictionary.Add(component, tupleSelector.ToList());
							}
						}

						var commentSplits = comment.Split('\n');
						string targetComponent = null;
						var notes = new List<string>();
						foreach (var line in commentSplits)
						{
							var trimmedLine = line.Trim();
							if (targetComponent == null)
							{
								if (trimmedLine.StartsWith(":cl:", StringComparison.Ordinal))
								{
									targetComponent = trimmedLine[4..].Trim();
									if (targetComponent.Length == 0)
										targetComponent = "Core";
								}
								continue;
							}
							if (trimmedLine.StartsWith("/:cl:", StringComparison.Ordinal))
							{
								await CommitNotes(targetComponent, notes);
								targetComponent = null;
								notes.Clear();
								continue;
							}
							if (trimmedLine.Length == 0)
								continue;

							notes.Add(trimmedLine);
						}
					}

					var comments = await client.Issue.Comment.GetAllForIssue(RepoOwner, RepoName, fullPR.Number).ConfigureAwait(false);
					await Task.WhenAll(BuildNotesFromComment(fullPR.Body, fullPR.User), Task.WhenAll(comments.Select(x => BuildNotesFromComment(x.Body, x.User)))).ConfigureAwait(false);
				}

				var tasks = new List<Task>();
				foreach (var I in milestonePRs.Items)
					tasks.Add(GetReleaseNotesFromPR(I));

				var releases = await releasesTask.ConfigureAwait(false);

				Version highestReleaseVersion = null;
				Release highestRelease = null;
				foreach (var I in releases)
				{
					if (!Version.TryParse(I.TagName.Replace("tgstation-server-v", String.Empty), out var currentReleaseVersion))
					{
						Console.WriteLine("WARNING: Unable to determine version of release " + I.HtmlUrl);
						continue;
					}

					if (currentReleaseVersion.Major == version.Major && (highestReleaseVersion == null || currentReleaseVersion > highestReleaseVersion) && version != currentReleaseVersion)
					{
						highestReleaseVersion = currentReleaseVersion;
						highestRelease = I;
					}
				}

				if (highestReleaseVersion == null)
				{
					Console.WriteLine("Unable to determine highest release version for major version " + version.Major + "!");
					return 6;
				}

				var oldNotes = highestRelease.Body;

				var splits = new List<string>(oldNotes.Split('\n'));
				//trim away all the lines that don't start with #

				string keepThisRelease;
				if (version.Build <= 1)
					keepThisRelease = "# ";
				else
					keepThisRelease = "## ";

				for (; !splits[0].StartsWith(keepThisRelease, StringComparison.Ordinal); splits.RemoveAt(0))
					if (splits.Count == 1)
					{
						Console.WriteLine("Error formatting release notes: Can't detemine notes start!");
						return 7;
					}

				oldNotes = String.Join('\n', splits);

				string prefix;
				const string PropsPath = "build/Version.props";
				const string ControlPanelPropsPath = "build/ControlPanelVersion.props";

				var doc = XDocument.Load(PropsPath);
				var project = doc.Root;
				var xmlNamespace = project.GetDefaultNamespace();
				var versionsPropertyGroup = project.Elements().First(x => x.Name == xmlNamespace + "PropertyGroup");

				var doc2 = XDocument.Load(ControlPanelPropsPath);
				var project2 = doc2.Root;
				var controlPanelXmlNamespace = project2.GetDefaultNamespace();
				var controlPanelVersionsPropertyGroup = project2.Elements().First(x => x.Name == controlPanelXmlNamespace + "PropertyGroup");

				var coreVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsCoreVersion").Value);
				if (coreVersion != version)
				{
					Console.WriteLine("Received a different version on command line than in Version.props!");
					return 10;
				}

				var apiVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsApiVersion").Value);
				var configVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsConfigVersion").Value);
				var dmApiVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsDmapiVersion").Value);
				var interopVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsInteropVersion").Value);
				var webControlVersion = Version.Parse(controlPanelVersionsPropertyGroup.Element(controlPanelXmlNamespace + "TgsControlPanelVersion").Value);
				var hostWatchdogVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsHostWatchdogVersion").Value);

				if (webControlVersion.Major == 0)
					postControlPanelMessage = true;

				prefix = $"Please refer to the [README](https://github.com/tgstation/tgstation-server#setup) for setup instructions.{Environment.NewLine}{Environment.NewLine}#### Component Versions\nCore: {coreVersion}\nConfiguration: {configVersion}\nHTTP API: {apiVersion}\nDreamMaker API: {dmApiVersion} (Interop: {interopVersion})\n[Web Control Panel](https://github.com/tgstation/tgstation-server-webpanel): {webControlVersion}\nHost Watchdog: {hostWatchdogVersion}";

				var newNotes = new StringBuilder(prefix);
				if (postControlPanelMessage)
				{
					newNotes.Append(Environment.NewLine);
					newNotes.Append(Environment.NewLine);
					newNotes.Append("### The recommended client is currently the legacy [Tgstation.Server.ControlPanel](https://github.com/tgstation/Tgstation.Server.ControlPanel/releases/latest). This will be phased out as the web client is completed.");
				}

				newNotes.Append(Environment.NewLine);
				newNotes.Append(Environment.NewLine);
				if (version.Build == 0)
				{
					newNotes.Append("# [Update ");
					newNotes.Append(version.Minor);
					newNotes.Append(".X");
				}
				else
				{
					newNotes.Append("## [Patch ");
					newNotes.Append(version.Build);
				}
				newNotes.Append("](");
				var milestone = await milestoneTask.ConfigureAwait(false);
				if (milestone == null)
				{
					Console.WriteLine("Unable to detemine milestone!");
					return 9;
				}

				if (doNotCloseMilestone)
					Console.WriteLine("Not closing milestone due to parameter!");
				else
				{
					Console.WriteLine("Closing milestone...");
					tasks.Add(client.Issue.Milestone.Update(RepoOwner, RepoName, milestone.Number, new MilestoneUpdate
					{
						State = ItemState.Closed
					}));

					// Create the next patch milestone
					var nextPatchMilestoneName = $"v{version.Major}.{version.Minor}.{version.Build + 1}";
					Console.WriteLine($"Creating milestone {nextPatchMilestoneName}...");
					var nextPatchMilestone = await client.Issue.Milestone.Create(
						RepoOwner,
						RepoName,
						new NewMilestone(nextPatchMilestoneName)
						{
							Description = "Next patch version"
						});

					if (version.Build == 0)
					{
						// close the patch milestone if it exists
						var milestones = await client.Issue.Milestone.GetAllForRepository(RepoOwner, RepoName, new MilestoneRequest
						{
							State = ItemStateFilter.Open
						});

						var milestoneToDelete = milestones.FirstOrDefault(x => x.Title.StartsWith($"v{highestReleaseVersion.Major}.{highestReleaseVersion.Minor}."));
						if (milestoneToDelete != null)
						{
							Console.WriteLine($"Moving {milestoneToDelete.OpenIssues} open issues and {milestoneToDelete.ClosedIssues} closed issues from unused patch milestone {milestoneToDelete.Title} to upcoming ones and deleting...");
							if (milestoneToDelete.OpenIssues + milestoneToDelete.ClosedIssues > 0)
							{
								var issuesInUnusedMilestone = await client.Search.SearchIssues(new SearchIssuesRequest
								{
									Milestone = milestoneToDelete.Title,
									Repos = { { RepoOwner, RepoName } }
								});

								var issueUpdateTasks = new List<Task>();
								foreach (var I in issuesInUnusedMilestone.Items)
								{
									issueUpdateTasks.Add(client.Issue.Update(RepoOwner, RepoName, I.Number, new IssueUpdate
									{
										Milestone = I.State.Value == ItemState.Closed ? milestone.Number : nextPatchMilestone.Number
									}));

									if (I.PullRequest != null)
									{
										Console.WriteLine($"Adding additional merged PR #{I.Number}...");
										tasks.Add(GetReleaseNotesFromPR(I));
									}
								}

								await Task.WhenAll(issueUpdateTasks).ConfigureAwait(false);
							}

							tasks.Add(client.Issue.Milestone.Delete(RepoOwner, RepoName, milestoneToDelete.Number));
						}

						// Create the next minor milestone
						var nextMinorMilestoneName = $"v{version.Major}.{version.Minor + 1}.0";
						Console.WriteLine($"Creating milestone {nextMinorMilestoneName}...");
						var nextMinorMilestoneTask = client.Issue.Milestone.Create(
							RepoOwner,
							RepoName,
							new NewMilestone(nextMinorMilestoneName)
							{
								Description = "Next minor version"
							});
						tasks.Add(nextMinorMilestoneTask);

						// Move unfinished stuff to new minor milestone
						Console.WriteLine($"Moving {milestone.OpenIssues} abandoned issue(s) from previous milestone to new one...");
						var abandonedIssues = await client.Search.SearchIssues(new SearchIssuesRequest
						{
							Milestone = milestone.Title,
							Repos = { { RepoOwner, RepoName } },
							State = ItemState.Open
						});

						if (abandonedIssues.Items.Any())
						{
							var nextMinorMilestone = await nextMinorMilestoneTask.ConfigureAwait(false);
							foreach (var I in abandonedIssues.Items)
								tasks.Add(client.Issue.Update(RepoOwner, RepoName, I.Number, new IssueUpdate
								{
									Milestone = nextMinorMilestone.Number
								}));
						}
					}
				}

				newNotes.Append(milestone.HtmlUrl);
				newNotes.Append("?closed=1)");
				newNotes.Append(Environment.NewLine);

				await Task.WhenAll(tasks).ConfigureAwait(false);

				if (releaseDictionary.Count == 0)
				{
					Console.WriteLine("No release notes for this milestone!");
					return 8;
				}

				foreach (var I in releaseDictionary.OrderBy(kvp => kvp.Key))
				{
					newNotes.Append(Environment.NewLine);
					newNotes.Append("#### ");
					newNotes.Append(I.Key);


					foreach (var noteTuple in I.Value)
					{
						newNotes.Append(Environment.NewLine);
						newNotes.Append("- ");
						newNotes.Append(noteTuple.Item1);
						newNotes.Append(" (#");
						newNotes.Append(noteTuple.Item2);
						newNotes.Append(" @");
						newNotes.Append(noteTuple.Item3);
						newNotes.Append(')');
					}

					newNotes.Append(Environment.NewLine);
				}

				newNotes.Append(Environment.NewLine);

				if (version.Minor != 0 && version.Build != 0)
					newNotes.Append(oldNotes);

				const string OutputPath = "release_notes.md";
				Console.WriteLine($"Writing out new release notes to {Path.GetFullPath(OutputPath)}...");
				var releaseNotes = newNotes.ToString();
				await File.WriteAllTextAsync(OutputPath, releaseNotes).ConfigureAwait(false);


				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return 4;
			}
		}
	}
}
