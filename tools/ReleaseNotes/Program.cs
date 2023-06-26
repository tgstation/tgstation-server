using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Octokit;
using Octokit.GraphQL;

namespace ReleaseNotes
{
	/// <summary>
	/// Contains the application entrypoint
	/// </summary>
	static class Program
	{
		const string RepoOwner = "tgstation";
		const string RepoName = "tgstation-server";

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
			var ensureRelease = versionString.Equals("--ensure-release", StringComparison.OrdinalIgnoreCase);
			var linkWinget = versionString.Equals("--link-winget", StringComparison.OrdinalIgnoreCase);
			var shaCheck = versionString.Equals("--winget-template-check", StringComparison.OrdinalIgnoreCase);

			if ((!Version.TryParse(versionString, out var version) || version.Revision != -1) && !ensureRelease && !linkWinget && !shaCheck)
			{
				Console.WriteLine("Invalid version: " + versionString);
				return 2;
			}

			var doNotCloseMilestone = args.Length > 1 && args[1].ToUpperInvariant() == "--NO-CLOSE";

			const string ReleaseNotesEnvVar = "TGS_RELEASE_NOTES_TOKEN";
			var githubToken = Environment.GetEnvironmentVariable(ReleaseNotesEnvVar);
			if (String.IsNullOrWhiteSpace(githubToken) && !doNotCloseMilestone && !ensureRelease)
			{
				Console.WriteLine("Missing " + ReleaseNotesEnvVar + " environment variable!");
				return 3;
			}

			var client = new GitHubClient(new Octokit.ProductHeaderValue("tgs_release_notes"));
			if (!String.IsNullOrWhiteSpace(githubToken))
			{
				client.Credentials = new Credentials(githubToken);
			}

			try
			{
				if (ensureRelease)
					return await EnsureRelease(client);

				if (linkWinget)
				{
					if (args.Length < 2 || !Uri.TryCreate(args[1], new UriCreationOptions(), out var actionsUrl))
					{
						Console.WriteLine("Missing/Invalid actions URL!");
						return 30;
					}

					return await Winget(client, actionsUrl, null);
				}

				if (shaCheck)
				{
					if(args.Length < 2)
					{
						Console.WriteLine("Missing SHA for PR template!");
						return 32;
					}

					return await Winget(client, null, args[1]);
				}

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

					void BuildNotesFromComment(string comment, User user)
					{
						if (comment == null)
							return;

						void CommitNotes(string component, List<string> notes)
						{
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
								CommitNotes(targetComponent, notes);
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
					BuildNotesFromComment(fullPR.Body, fullPR.User);
					foreach(var x in comments)
						BuildNotesFromComment(x.Body, x.User);
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

					if (currentReleaseVersion.Major > 3 && (highestReleaseVersion == null || currentReleaseVersion > highestReleaseVersion) && version != currentReleaseVersion)
					{
						highestReleaseVersion = currentReleaseVersion;
						highestRelease = I;
					}
				}

				if (highestReleaseVersion == null)
				{
					Console.WriteLine("Unable to determine highest release version!");
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

				Console.WriteLine("Updating Server Release Thread...");
				var productInformation = new Octokit.GraphQL.ProductHeaderValue("tgs_release_notes");
				var connection = new Octokit.GraphQL.Connection(productInformation, githubToken);

				var mutation = new Mutation()
					.AddDiscussionComment(new Octokit.GraphQL.Model.AddDiscussionCommentInput
					{
						Body = $"[tgstation-server-v{versionString}](https://github.com/tgstation/tgstation-server/releases/tag/tgstation-server-v{versionString}) released.",
						DiscussionId = new ID("MDEwOkRpc2N1c3Npb24zNTU5OTUx")
					})
					.Select(payload => new
					{
						payload.ClientMutationId
					})
					.Compile();

				await connection.Run(mutation);

				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return 4;
			}
		}

		class ExtendedReleaseUpdate : ReleaseUpdate
		{
			public bool? MakeLatest { get; set; }
		}

		static async Task<int> EnsureRelease(IGitHubClient client)
		{
			Console.WriteLine("Ensuring latest release is a GitHub release...");
			var latestRelease = await client.Repository.Release.GetLatest(RepoOwner, RepoName);

			const string TagPrefix = "tgstation-server-v";
			static bool IsServerRelease(Release release) => release.TagName.StartsWith(TagPrefix);

			if (!IsServerRelease(latestRelease))
			{
				var allReleases = await client.Repository.Release.GetAll(RepoOwner, RepoName);
				var orderedReleases = allReleases
					.Where(IsServerRelease)
					.OrderByDescending(x => Version.Parse(x.TagName[TagPrefix.Length..]));
				latestRelease = orderedReleases
					.First();

				// this should set it as latest
				await client.Repository.Release.Edit(RepoOwner, RepoName, latestRelease.Id, new ExtendedReleaseUpdate
				{
					MakeLatest = true
				});
			}

			return 0;
		}

		static async Task<int> Winget(IGitHubClient client, Uri actionUrl, string expectedTemplateSha)
		{
			const string PropsPath = "build/Version.props";

			var doc = XDocument.Load(PropsPath);
			var project = doc.Root;
			var xmlNamespace = project.GetDefaultNamespace();
			var versionsPropertyGroup = project.Elements().First(x => x.Name == xmlNamespace + "PropertyGroup");
			var coreVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsCoreVersion").Value);

			const string BodyForPRSha = "596da68f8da0926ae17a1497328e368d7b83aac2";
			var prBody = $@"# Automated Pull Request

This pull request was generated by our [deployment pipeline]({actionUrl}) as a result of the release of [tgstation-server-v{coreVersion}](https://github.com/tgstation/tgstation-server/releases/tag/tgstation-server-v{coreVersion}). Validation was performed as part of the process.

The user account that created this pull request is available to correct any issues.

- [x] Have you signed the [Contributor License Agreement](https://cla.opensource.microsoft.com/microsoft/winget-pkgs)?
- [x] Have you checked that there aren't other open [pull requests](https://github.com/microsoft/winget-pkgs/pulls) for the same manifest update/change?
  - This PR is generated as a direct result of a new release of `tgstation-server` this should be impossible
- [x] This PR only modifies one (1) manifest
- [x] Have you [validated](https://github.com/microsoft/winget-pkgs/blob/master/AUTHORING_MANIFESTS.md#validation) your manifest locally with `winget validate --manifest <path>`?
  - Validation is performed as a prerequisite to deployment.
- [x] Have you tested your manifest locally with `winget install --manifest <path>`?
  - Manifest installation and uninstallation is performed as a prerequisite to deployment.
- [x] Does your manifest conform to the [1.4 schema](https://github.com/microsoft/winget-pkgs/tree/master/doc/manifest/schema/1.4.0)?";

			if (expectedTemplateSha != null)
			{
				if (expectedTemplateSha != BodyForPRSha)
				{
					Console.WriteLine("winget-pkgs pull request template has updated. This tool will need to be updated to match!");
					Console.WriteLine($"Expected {BodyForPRSha} found {expectedTemplateSha}");
					return 33;
				}

				return 0;
			}

			var clientUser = await client.User.Current();

			var userPrsOnWingetRepo = await client.Search.SearchIssues(new SearchIssuesRequest
			{
				Author = clientUser.Name,
				Is = new List<IssueIsQualifier> { IssueIsQualifier.PullRequest },
				State = ItemState.Open,
				Repos = new RepositoryCollection
				{
					{ "microsoft", "winget-pkgs" },
				},
			});

			var prToModify = userPrsOnWingetRepo.Items.OrderByDescending(pr => pr.Number).FirstOrDefault();
			if(prToModify == null)
			{
				Console.WriteLine("Could not find open winget-pkgs PR!");
				return 31;
			}

			await client.Issue.Update(prToModify.Repository.Id, prToModify.Number, new IssueUpdate
			{
				Body = prBody,
			});

			return 0;
		}
	}
}
