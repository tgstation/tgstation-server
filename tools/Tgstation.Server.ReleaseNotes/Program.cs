// This program is minimal effort and should be sent to remedial school

using System;
using System.Buffers.Text;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.IdentityModel.Tokens;

using Newtonsoft.Json;

using Octokit;
using Octokit.GraphQL;

using Tgstation.Server.Shared;

using YamlDotNet.Serialization;

namespace Tgstation.Server.ReleaseNotes
{
	/// <summary>
	/// Contains the application entrypoint
	/// </summary>
	static class Program
	{
		const string OutputPath = "release_notes.md";

		// some stuff that should be abstracted for different repos
		const string RepoOwner = "tgstation";
		const string RepoName = "tgstation-server";
		const int AppId = 847638;

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
			var fullNotes = versionString.Equals("--generate-full-notes", StringComparison.OrdinalIgnoreCase);
			var nuget = versionString.Equals("--nuget", StringComparison.OrdinalIgnoreCase);
			var ciCompletionCheck = versionString.Equals("--ci-completion-check", StringComparison.OrdinalIgnoreCase);
			var genToken = versionString.Equals("--token-output-file", StringComparison.OrdinalIgnoreCase);

			if ((!Version.TryParse(versionString, out var version) || version.Revision != -1)
				&& !ensureRelease
				&& !linkWinget
				&& !shaCheck
				&& !fullNotes
				&& !nuget
				&& !ciCompletionCheck
				&& !genToken)
			{
				Console.WriteLine("Invalid version: " + versionString);
				return 2;
			}

			var doNotCloseMilestone = false;
			var debianMode = false;
			Component? componentRelease = null;
			if (args.Length > 1)
				switch (args[1].ToUpperInvariant())
				{
					case "--DEBIAN":
						debianMode = true;
						doNotCloseMilestone = true;
						if (args.Length < 3)
						{
							Console.WriteLine("Missing output path!");
							return 238;
						}

						if (args.Length < 4)
						{
							Console.WriteLine("Missing current SHA!");
							return 239;
						}
						break;
					case "--NO-CLOSE":
						doNotCloseMilestone = true;
						break;
					case "--HTTPAPI":
						componentRelease = Component.HttpApi;
						break;
					case "--INTEROPAPI":
						componentRelease = Component.InteropApi;
						break;
					case "--DMAPI":
						componentRelease = Component.DreamMakerApi;
						break;
				}

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
				{
					if (args.Length < 2)
					{
						Console.WriteLine("Missing PEM Base64 for updating release!");
						return 454233;
					}

					await GenerateAppCredentials(client, args[1]);

					return await EnsureRelease(client);
				}

				if (linkWinget)
				{
					if (args.Length < 2 || !Uri.TryCreate(args[1], new UriCreationOptions(), out var actionsUrl))
					{
						Console.WriteLine("Missing/Invalid actions URL!");
						return 30;
					}

					return await Winget(client, actionsUrl, null);
				}

				if (ciCompletionCheck)
				{
					if (args.Length < 3)
					{
						Console.WriteLine("Missing SHA or PEM Base64 for creating check run!");
						return 4543;
					}

					return await CICompletionCheck(client, args[1], args[2]);
				}


				if (genToken)
				{
					if (args.Length < 3)
					{
						Console.WriteLine("Missing output file path or PEM Base64 for app authentication!");
						return 33847;
					}

					await GenerateAppCredentials(client, args[2]);

					var token = client.Credentials.GetToken();
					var destPath = args[1];
					Directory.CreateDirectory(Path.GetDirectoryName(destPath));
					await File.WriteAllTextAsync(destPath, token);
					return 0;
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

				if (fullNotes)
					return await FullNotes(client);

				if (componentRelease.HasValue)
					return await ReleaseComponent(client, version, componentRelease.Value);

				if (nuget)
					return await ReleaseNuget(client);

				if (debianMode)
					return await GenDebianChangelog(client, version, args[2], args[3]);

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

				bool postControlPanelMessage = false;

				var noteTasks = new List<Task<Tuple<Dictionary<Component, Changelist>, Dictionary<Component, Version>, bool>>>();

				foreach (var I in milestonePRs.Items)
					noteTasks.Add(GetReleaseNotesFromPR(client, I, doNotCloseMilestone, false, false));

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
				const string ControlPanelPropsPath = "build/WebpanelVersion.props";

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
				var webControlVersion = Version.Parse(controlPanelVersionsPropertyGroup.Element(controlPanelXmlNamespace + "TgsWebpanelVersion").Value);
				var hostWatchdogVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsHostWatchdogVersion").Value);

				if (webControlVersion.Major == 0)
					postControlPanelMessage = true;

				prefix = $"Please refer to the [README](https://github.com/tgstation/tgstation-server#setup) for setup instructions. Full changelog can be found [here](https://raw.githubusercontent.com/tgstation/tgstation-server/gh-pages/changelog.yml).{Environment.NewLine}{Environment.NewLine}#### Component Versions\nCore: {coreVersion}\nConfiguration: {configVersion}\nHTTP API: {apiVersion}\nDreamMaker API: {dmApiVersion} (Interop: {interopVersion})\n[Web Control Panel](https://github.com/tgstation/tgstation-server-webpanel): {webControlVersion}\nHost Watchdog: {hostWatchdogVersion}";

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

				await Task.WhenAll(noteTasks);

				var milestone = milestones.Single().Value;
				if (milestone == null)
				{
					Console.WriteLine("Unable to detemine milestone!");
					return 9;
				}

				var allTasks = new List<Task>(noteTasks);
				if (doNotCloseMilestone)
					Console.WriteLine("Not closing milestone due to parameter!");
				else
				{
					Console.WriteLine("Closing milestone...");
					allTasks.Add(client.Issue.Milestone.Update(RepoOwner, RepoName, milestone.Number, new MilestoneUpdate
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

						async ValueTask DeleteMilestone(Milestone milestoneToDelete, int moveToMilestoneNumber)
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
									if (I.State.Value != ItemState.Closed)
										issueUpdateTasks.Add(client.Issue.Update(RepoOwner, RepoName, I.Number, new IssueUpdate
										{
											Milestone = moveToMilestoneNumber
										}));

									if (I.PullRequest != null)
									{
										Console.WriteLine($"Adding additional merged PR #{I.Number}...");
										var task = GetReleaseNotesFromPR(client, I, doNotCloseMilestone, false, false);
										noteTasks.Add(task);
										allTasks.Add(task);
									}
								}

								await Task.WhenAll(issueUpdateTasks).ConfigureAwait(false);
							}

							allTasks.Add(client.Issue.Milestone.Delete(RepoOwner, RepoName, milestoneToDelete.Number));
						}


						var unreleasedNextPatchMilestone = milestones.FirstOrDefault(x => x.Title.StartsWith($"v{highestReleaseVersion.Major}.{highestReleaseVersion.Minor}."));
						if (unreleasedNextPatchMilestone != null)
							await DeleteMilestone(unreleasedNextPatchMilestone, nextPatchMilestone.Number);

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
						allTasks.Add(nextMinorMilestoneTask);

						// Move unfinished stuff to new minor milestone
						Console.WriteLine($"Moving {milestone.OpenIssues} abandoned issue(s) from previous milestone to new one...");
						var abandonedIssues = await client.Search.SearchIssues(new SearchIssuesRequest
						{
							Milestone = milestone.Title,
							Repos = { { RepoOwner, RepoName } },
							State = ItemState.Open
						});

						var nextMinorMilestone = await nextMinorMilestoneTask.ConfigureAwait(false);
						if (abandonedIssues.Items.Any())
						{
							foreach (var I in abandonedIssues.Items)
								allTasks.Add(client.Issue.Update(RepoOwner, RepoName, I.Number, new IssueUpdate
								{
									Milestone = nextMinorMilestone.Number
								}));
						}

						if (version.Minor == 0 && version.Build == 0)
						{
							// major release
							var unreleasedNextMinorMilestone = milestones.FirstOrDefault(x => x.Title.StartsWith($"v{highestReleaseVersion.Major}.{highestReleaseVersion.Minor + 1}.0"));
							if (unreleasedNextMinorMilestone != null)
								await DeleteMilestone(unreleasedNextMinorMilestone, nextMinorMilestone.Number);
						}
					}
				}

				newNotes.Append(milestone.HtmlUrl);
				newNotes.Append("?closed=1)");
				newNotes.Append(Environment.NewLine);

				await Task.WhenAll(allTasks).ConfigureAwait(false);

				var componentVersionDict = new Dictionary<Component, Version>
				{
					{ Component.Configuration, configVersion },
					{ Component.HttpApi, apiVersion },
					{ Component.DreamMakerApi, dmApiVersion },
					{ Component.InteropApi, interopVersion },
					{ Component.WebControlPanel, webControlVersion },
					{ Component.HostWatchdog, hostWatchdogVersion },
				};

				var releaseDictionary = new SortedDictionary<Component, Changelist>(
					new Dictionary<Component, Changelist>(
						noteTasks
							.Where(task => task.Result != null)
							.SelectMany(task => task.Result.Item1)
							.Where(kvp => kvp.Key == Component.Core || componentVersionDict.ContainsKey(kvp.Key))
							.GroupBy(kvp => kvp.Key)
							.Select(grouping =>
							{
								var component = grouping.Key;
								var changelist = new Changelist
								{
									Changes = grouping.SelectMany(kvp => kvp.Value.Changes).ToList()
								};

								if (component == Component.Core)
								{
									changelist.Version = coreVersion;
									changelist.ComponentVersions = componentVersionDict;
								}
								else
									changelist.Version = componentVersionDict[component];

								return new KeyValuePair<Component, Changelist>(component, changelist);
							})));

				if (releaseDictionary.Count == 0)
				{
					Console.WriteLine("No release notes for this milestone!");
					return 8;
				}

				foreach (var I in releaseDictionary)
				{
					newNotes.Append(Environment.NewLine);
					newNotes.Append("#### ");
					string componentName = GetComponentDisplayName(I.Key, false);
					newNotes.Append(componentName);

					if (I.Key == Component.Configuration)
					{
						I.Value.StripConfigVersionMessage();
						newNotes.AppendLine();
						newNotes.Append("- **The new configuration version is `");
						newNotes.Append(I.Value.Version);
						newNotes.Append("`. Please update your `General:ConfigVersion` setting appropriately.**");
					}

					PrintChanges(newNotes, I.Value);

					newNotes.Append(Environment.NewLine);
				}

				newNotes.Append(Environment.NewLine);

				if (version.Minor != 0 && version.Build != 0)
					newNotes.Append(oldNotes);

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

				if (!doNotCloseMilestone)
					await connection.Run(mutation);

				return 0;
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				return 4;
			}
		}

		static string GetComponentDisplayName(Component component, bool debian) => component switch
		{
			Component.HttpApi => debian ? "the HTTP API" : "HTTP API",
			Component.InteropApi => debian ? "the Interop API" : "Interop API",
			Component.Configuration => debian ? "the TGS configuration" : "**Configuration**",
			Component.DreamMakerApi => debian ? "the DreamMaker API" : "DreamMaker API",
			Component.HostWatchdog => debian ? "the Host Watchdog" : "Host Watchdog",
			Component.Core => debian ? "the main server" : "Core",
			Component.WebControlPanel => debian ? "the Web Control Panel" : "Web Control Panel",
			_ => throw new Exception($"Unnamed Component: {component}"),
		};

		static readonly ConcurrentDictionary<int, Milestone> milestones = new();
		static readonly ConcurrentDictionary<int, Task<PullRequest>> pullRequests = new();

		static Task<PullRequest> GetPR(IGitHubClient client, int pr) => pullRequests.GetOrAdd(pr, x => RLR(() => client.Repository.PullRequest.Get(RepoOwner, RepoName, x)));

		static async Task<Tuple<Dictionary<Component, Changelist>, Dictionary<Component, Version>, bool>> GetReleaseNotesFromPR(IGitHubClient client, Issue pullRequest, bool doNotCloseMilestone, bool needComponentExactVersions, bool forAllComponents)
		{
			//need to check it was merged
			var prTask = GetPR(client, pullRequest.Number);
			var fullPR = await prTask;

			if (!fullPR.Merged)
			{
				if (!doNotCloseMilestone && fullPR.Milestone != null)
				{
					Console.WriteLine($"Removing trash PR #{fullPR.Number} from milestone...");
					await RLR(() => client.Issue.Update(RepoOwner, RepoName, fullPR.Number, new IssueUpdate
					{
						Milestone = null
					}));
				}

				return null;
			}

			if (fullPR.Milestone == null)
			{
				return null;
			}

			milestones.TryAdd(fullPR.Milestone.Number, fullPR.Milestone);

			var commentsTask = TripleCheckGitHubPagination(apiOptions => client.Issue.Comment.GetAllForIssue(fullPR.Base.Repository.Id, pullRequest.Number, apiOptions), comment => comment.Id);

			bool isReleasePR = false;
			async Task<bool> ShouldGetExtendedComponentVersions()
			{
				if (forAllComponents)
					return true;

				var commit = await RLR(() => client.Repository.Commit.Get(fullPR.Base.Repository.Id, fullPR.MergeCommitSha));

				isReleasePR = commit.Commit.Message.Contains("[TGSDeploy]")
					|| fullPR.Number == 966
					|| fullPR.Number == 1048
					|| fullPR.Number == 1435
					|| fullPR.Number == 1263
					|| fullPR.Number == 1087
					|| fullPR.Number == 1441
					|| fullPR.Number == 1437
					|| fullPR.Number == 1443
					|| fullPR.Number == 1311
					|| fullPR.Number == 1598
					|| fullPR.Number == 1463
					|| fullPR.Number == 1209; // some special tactics from before we were more stingent

				return isReleasePR;
			}

			Task<bool> needExtendedComponentVersions = Task.FromResult(false);
			async Task<Dictionary<Component, Version>> GetComponentVersions()
			{
				var mergeCommit = fullPR.MergeCommitSha;
				// we don't care about unreleased web control panel changes

				needExtendedComponentVersions = ShouldGetExtendedComponentVersions();

				var versionsBytes = await RLR(() => client.Repository.Content.GetRawContentByRef(RepoOwner, RepoName, "build/Version.props", mergeCommit));

				XDocument doc;
				using (var ms = new MemoryStream(versionsBytes))
					doc = XDocument.Load(ms);

				var project = doc.Root;
				var xmlNamespace = project.GetDefaultNamespace();
				var versionsPropertyGroup = project.Elements().First(x => x.Name == xmlNamespace + "PropertyGroup");

				Version Parse(string elemName, bool controlPanel = false)
				{
					var element = versionsPropertyGroup.Element(xmlNamespace + elemName);
					if (element == null)
						return null;

					return Version.Parse(element.Value);
				}

				var dict = new Dictionary<Component, Version>
				{
					{ Component.Core, Parse("TgsCoreVersion") },
					{ Component.HttpApi, Parse("TgsApiVersion") },
					{ Component.DreamMakerApi, Parse("TgsDmapiVersion") },
				};

				if (await needExtendedComponentVersions)
				{
					// only grab some versions at release time
					// we aggregate later
					dict.Add(Component.Configuration, Parse("TgsConfigVersion"));
					dict.Add(Component.InteropApi, Parse("TgsInteropVersion"));
					dict.Add(Component.HostWatchdog, Parse("TgsHostWatchdogVersion"));
					dict.Add(Component.NugetCommon, Parse("TgsCommonLibraryVersion"));
					dict.Add(Component.NugetApi, Parse("TgsApiLibraryVersion"));
					dict.Add(Component.NugetClient, Parse("TgsClientVersion"));

					var webVersion = Parse("TgsControlPanelVersion");
					if (webVersion != null)
					{
						dict.Add(Component.WebControlPanel, webVersion);
					}
					else
					{
						byte[] controlPanelVersionBytes;
						string elementName;
						try
						{
							controlPanelVersionBytes = await RLR(() => client.Repository.Content.GetRawContentByRef(RepoOwner, RepoName, "build/WebpanelVersion.props", mergeCommit));
							elementName = "TgsWebpanelVersion";
						}
						catch (NotFoundException)
						{
							controlPanelVersionBytes = await RLR(() => client.Repository.Content.GetRawContentByRef(RepoOwner, RepoName, "build/ControlPanelVersion.props", mergeCommit));
							elementName = "TgsControlPanelVersion";
						}

						using (var ms = new MemoryStream(controlPanelVersionBytes))
							doc = XDocument.Load(ms);

						project = doc.Root;
						var controlPanelXmlNamespace = project.GetDefaultNamespace();
						var controlPanelVersionsPropertyGroup = project.Elements().First(x => x.Name == controlPanelXmlNamespace + "PropertyGroup");
						dict.Add(Component.WebControlPanel, Version.Parse(controlPanelVersionsPropertyGroup.Element(controlPanelXmlNamespace + elementName).Value));
					}
				}

				return dict;
			}

			var componentVersions = needComponentExactVersions ? GetComponentVersions() : Task.FromResult<Dictionary<Component, Version>>(null);
			var changelists = new ConcurrentDictionary<Component, Changelist>();
			async Task BuildNotesFromComment(string comment, User user, Task localPreviousTask)
			{
				await localPreviousTask;
				if (comment == null)
					return;

				async Task CommitNotes(Component component, List<string> notes)
				{
					foreach (var I in notes)
						Console.WriteLine(component + " #" + fullPR.Number + " - " + I + " (@" + user.Login + ")");

					var tupleSelector = notes.Select(note => new Change
					{
						Descriptions = new List<string> { note },
						PullRequest = fullPR.Number,
						Author = user.Login
					});

					var useExtendedComponentVersions = await needExtendedComponentVersions;
					var componentVersionsResult = await componentVersions;
					lock (changelists)
						if (changelists.TryGetValue(component, out var currentChangelist))
							currentChangelist.Changes.AddRange(tupleSelector);
						else
							DebugAssert(changelists.TryAdd(component, new Changelist
							{
								Changes = tupleSelector.ToList(),
								Unreleased = false,
								Version = needComponentExactVersions && componentVersionsResult.TryGetValue(component, out var componentVersion)
									? componentVersion
									: null,
								ComponentVersions = component == Component.Core && needComponentExactVersions && useExtendedComponentVersions
									? new Dictionary<Component, Version>(componentVersionsResult.Where(kvp => kvp.Key != Component.Core))
									: null
							}));
				}

				var commentSplits = comment.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
				string targetComponent = null;
				var notes = new List<string>();
				foreach (var line in commentSplits)
				{
					var trimmedLine = line.Trim();
					if (targetComponent == null)
					{
						if (trimmedLine.StartsWith(":cl:", StringComparison.Ordinal) || trimmedLine.StartsWith("🆑", StringComparison.Ordinal))
						{
							var matchLength = trimmedLine.StartsWith("🆑", StringComparison.Ordinal)
								? "🆑".Length
								: 4;

							targetComponent = trimmedLine[matchLength..].Trim();
							if (targetComponent.Length == 0)
								targetComponent = "Core";
						}
						continue;
					}
					if (trimmedLine.StartsWith("/:cl:", StringComparison.Ordinal) || trimmedLine.StartsWith("/🆑", StringComparison.Ordinal))
					{
						if(!Enum.TryParse<Component>(targetComponent, out var component))
							component = targetComponent.ToUpperInvariant() switch
							{
								"**CONFIGURATION**" or "CONFIGURATION" or "CONFIG" => Component.Configuration,
								"HTTP API" => Component.HttpApi,
								"WEB CONTROL PANEL" => Component.WebControlPanel,
								"DMAPI" or "DREAMMAKER API" => Component.DreamMakerApi,
								"INTEROP API" => Component.InteropApi,
								"HOST WATCHDOG" => Component.HostWatchdog,
								"NUGET: API" => Component.NugetApi,
								"NUGET: COMMON" => Component.NugetCommon,
								"NUGET: CLIENT" => Component.NugetClient,
								_ => throw new Exception($"Unknown component: \"{targetComponent}\""),
							};
						await CommitNotes(component, notes);
						targetComponent = null;
						notes.Clear();
						continue;
					}
					if (trimmedLine.Length == 0)
						continue;

					notes.Add(trimmedLine);
				}
			}

			var previousTask = BuildNotesFromComment(fullPR.Body, fullPR.User, Task.CompletedTask);
			var comments = await commentsTask;
			foreach (var x in comments)
				previousTask = BuildNotesFromComment(x.Body, x.User, previousTask);

			await previousTask;

			DebugAssert(!(await needExtendedComponentVersions) || changelists.Where(x => x.Key == Component.Core).All(x => x.Value.ComponentVersions != null && x.Value.ComponentVersions.Count > 3));

			return Tuple.Create(changelists.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), await componentVersions, isReleasePR);
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
				await client.Repository.Release.Edit(RepoOwner, RepoName, latestRelease.Id, new ReleaseUpdate
				{
					MakeLatest = MakeLatestQualifier.True
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

			const string BodyForPRSha = "5ffc3ff5901db66d782aa0e8ed2a74b16f896091";
			var prBody = $@"# Automated Pull Request

This pull request was generated by our [deployment pipeline]({actionUrl}) as a result of the release of [tgstation-server-v{coreVersion}](https://github.com/tgstation/tgstation-server/releases/tag/tgstation-server-v{coreVersion}). Validation was performed as part of the process.

The user account that created this pull request is available to correct any issues.

Checklist for Pull Requests
- [x] Have you signed the [Contributor License Agreement](https://cla.opensource.microsoft.com/microsoft/winget-pkgs)?
- [x] Is there a linked Issue? **No**

Manifests
- [x] Have you checked that there aren't other open [pull requests](https://github.com/microsoft/winget-pkgs/pulls) for the same manifest update/change? **Impossible**
- [x] This PR only modifies one (1) manifest
- [x] Have you [validated](https://github.com/microsoft/winget-pkgs/blob/master/doc/Authoring.md#validation) your manifest locally with `winget validate --manifest <path>`?
- [x] Have you tested your manifest locally with `winget install --manifest <path>`?
- [x] Does your manifest conform to the [1.6 schema](https://github.com/microsoft/winget-pkgs/tree/master/doc/manifest/schema/1.6.0)?

Note: `<path>` is the directory's name containing the manifest you're submitting.

###### Microsoft Reviewers: [Open in CodeFlow](https://microsoft.github.io/open-pr/?codeflow=https://github.com/microsoft/winget-pkgs/pull/$PR_NUMBER_SUBST$)

---
";

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
				Author = clientUser.Login,
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

			await client.Issue.Update("microsoft", "winget-pkgs", prToModify.Number, new IssueUpdate
			{
				Body = prBody.Replace("$PR_NUMBER_SUBST$", prToModify.Number.ToString()),
			});
			return 0;
		}

		static async Task<T> RLR<T>(Func<Task<T>> func)
		{
			while (true)
				try
				{
					return await func();
				}
				catch (HttpRequestException ex) when (ex.InnerException is IOException ioEx && ioEx.InnerException is SocketException sockEx && sockEx.ErrorCode == 10053)
				{
					await Task.Delay(15000);
				}
				catch (SecondaryRateLimitExceededException)
				{
					await Task.Delay(15000);
				}
				catch (RateLimitExceededException ex)
				{
					var now = DateTimeOffset.UtcNow.AddSeconds(-10);
					if (ex.Reset > now)
					{
						var delay = ex.Reset - now;
						await Task.Delay(delay);
					}
				}
		}

		static async Task<List<T>> TripleCheckGitHubPagination<T>(Func<ApiOptions, Task<IReadOnlyList<T>>> apiCall, Func<T, object> idSelector)
		{
			// I've seen GitHub pagination return incomplete result sets in the past
			// It has an in-built pagination limit of 100
			var apiOptions = new ApiOptions
			{
				PageSize = 100
			};
			var results = await RLR(() => apiCall(apiOptions));
			var distinctEntries = new Dictionary<string, T>(results.Count);
			foreach (var result in results)
				distinctEntries.TryAdd(idSelector(result).ToString(), result);

			if (results.Count > 100)
			{
				results = await RLR(() => apiCall(apiOptions));
				foreach (var result in results)
					distinctEntries.TryAdd(idSelector(result).ToString(), result);

				results = await RLR(() => apiCall(apiOptions));
				foreach (var result in results)
					distinctEntries.TryAdd(idSelector(result).ToString(), result);
			}

			return distinctEntries.Values.ToList();
		}

		static async Task<Task<ReleaseNotes>> ProcessMilestone(IGitHubClient client, Milestone milestone)
		{
			// have to trust this works
			SearchIssuesResult results;

			var milestoneTask = Task.FromResult(milestone);
			var pullRequests = new Dictionary<int, Issue>();
			var iteration = 0;
			while (true)
			{
				results = await RLR(() => client.Search.SearchIssues(new SearchIssuesRequest
				{
					Type = IssueTypeQualifier.PullRequest,
					Milestone = milestone.Title,
					Repos = new RepositoryCollection
					{
						{ RepoOwner, RepoName },
					},
					Merged = DateRange.GreaterThan(new DateTimeOffset(2018, 9, 27, 0, 0, 0, TimeSpan.Zero)),
				}));

				foreach (var result in results.Items)
					pullRequests.TryAdd(result.Number, result);

				if (results.IncompleteResults)
					continue;

				if (results.TotalCount <= 100 || ++iteration == 3)
					break;
			}

			async Task<ReleaseNotes> RunPRs()
			{
				var milestoneVersion = Version.Parse(milestone.Title[1..]);
				var prTasks = pullRequests.Select(
					kvp => GetReleaseNotesFromPR(client, kvp.Value, true, true, milestone.State.Value == ItemState.Open))
					.ToList();

				await Task.WhenAll(prTasks);

				var prResults = prTasks.Select(x => x.Result).ToList();

				var releasePRResult = prResults.FirstOrDefault(x => x.Item3);

				prResults = prResults.Where(result => result != null).ToList();

				Dictionary<Component, Version> releasedComponentVersions;
				if (releasePRResult != null)
					releasedComponentVersions = releasePRResult.Item2;
				else
				{
					releasedComponentVersions = new Dictionary<Component, Version>(
						prResults
							.SelectMany(result => result.Item2)
							.GroupBy(kvp => kvp.Key)
							.Select(grouping => new KeyValuePair<Component, Version>(grouping.Key, grouping.Max(kvp => kvp.Value))));

					foreach(var maxVersionKvp in prResults.SelectMany(x => x.Item1)
						.Where(x => !releasedComponentVersions.ContainsKey(x.Key))
						.GroupBy(x => x.Key)
						.Select(group => {
							var versions = group
								.Where(x => x.Value.Version != null)
								.ToList();

							if (versions.Count == 0)
								return new KeyValuePair<Component, Version>(group.Key, null);

							return new KeyValuePair<Component, Version>(group.Key, versions.Max(x => x.Value.Version));
						})
						.Where(kvp => kvp.Value != null)
						.ToList())
					{
						releasedComponentVersions.Add(maxVersionKvp.Key, maxVersionKvp.Value);
					}
				}

				var finalResults = new Dictionary<Component, List<Changelist>>();
				foreach (var componentKvp in releasedComponentVersions)
				{
					var component = componentKvp.Key;
					var list = new List<Changelist>();

					foreach(var changelistDict in prResults.Select(x => x.Item1))
					{
						if (!changelistDict.TryGetValue(component, out var changelist))
							continue;

						Version componentVersion = milestoneVersion;
						var unreleased = milestone.State.Value == ItemState.Open;
						if (component != Component.Core)
						{
							componentVersion = changelist.Version ?? componentKvp.Value;
							if (releasedNonCoreVersions != null
								&& releasedNonCoreVersions.TryGetValue(component, out var releasedVersions)
								&& !releasedVersions.Any(x => x == componentVersion))
							{
								// roll forward
								var newList = releasedVersions
									.ToList();
								newList.Add(componentVersion);
								newList = newList.OrderBy(x => x).ToList();

								var index = newList.IndexOf(componentVersion);
								DebugAssert(index != -1);
								if (index != (newList.Count - 1))
								{
									componentVersion = newList[index + 1];
									unreleased = false;
								}
								else
									unreleased = true;
							}
						}

						var entry = list.FirstOrDefault(x => x.Version == componentVersion);
						if (entry == null)
						{
							entry = changelist;
							entry.Version = componentVersion;
							entry.Unreleased = unreleased;
							if (component == Component.Core && entry.ComponentVersions == null)
								entry.ComponentVersions = releasedComponentVersions;

							list.Add(entry);
						}
						else
							entry.Changes.AddRange(changelist.Changes);
					}

					DebugAssert(list.Select(x => x.Version.ToString()).Distinct().Count() == list.Count);
					if (component == Component.Core)
					{
						DebugAssert(list.All(x => x.Version == milestoneVersion));
					}

					list = list.OrderByDescending(x => x.Version).ToList();
					finalResults.Add(component, list);
				}

				if (!finalResults.ContainsKey(Component.Core) || finalResults[Component.Core].Count == 0)
				{
					finalResults.Remove(Component.Core);
					finalResults.Add(Component.Core, new List<Changelist>
					{
						new()
						{
							Changes = new List<Change>(),
							ComponentVersions = releasedComponentVersions,
							Unreleased = milestone.State.Value == ItemState.Open,
							Version = milestoneVersion,
						}
					});
				}
				else
					DebugAssert(finalResults[Component.Core].All(x => x.Version == milestoneVersion && x.ComponentVersions != null && x.ComponentVersions.Count > 3));

				var notes = new ReleaseNotes
				{
					Components = new SortedDictionary<Component, List<Changelist>>(finalResults),
				};

				return notes;
			}

			return RunPRs();
		}

		static async Task<int> FullNotes(IGitHubClient client)
		{
			var rateLimitInfo = client.GetLastApiInfo()?.RateLimit ?? (await client.RateLimit.GetRateLimits()).Rate;
			var startRateLimit = rateLimitInfo.Remaining;

			var releaseNotes = await GenerateNotes(client);

			Console.WriteLine($"Generating all release notes took {startRateLimit - client.GetLastApiInfo().RateLimit.Remaining} requests.");

			var serializer = new SerializerBuilder()
				.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
				.WithTypeConverter(new VersionConverter())
				.Build();

			var serializedYaml = serializer.Serialize(releaseNotes);
			await File.WriteAllTextAsync("changelog.yml", serializedYaml).ConfigureAwait(false);
			return 0;
		}

		static readonly HttpClient httpClient = new (
			new HttpClientHandler()
			{
				AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
			});
		static async Task<HashSet<Version>> EnumerateNugetVersions(string package)
		{
			var url = new Uri($"https://api.nuget.org/v3/registration5-gz-semver2/{package.ToLowerInvariant()}/index.json");

			using var req = new HttpRequestMessage();
			req.Headers.UserAgent.Add(new System.Net.Http.Headers.ProductInfoHeaderValue("Tgstation.Server.ReleaseNotes", "0.1.0"));
			req.Method = HttpMethod.Get;
			req.RequestUri = url;

			using var resp = await httpClient.SendAsync(req);
			resp.EnsureSuccessStatusCode();

			var json = await resp.Content.ReadAsStringAsync();

			dynamic dynamicJson = JsonConvert.DeserializeObject(json);

			var versions = (IEnumerable<dynamic>)dynamicJson.items[0].items;
			var results = versions
				.Select(x => Version.TryParse((string)x.catalogEntry.version, out var version) ? version : null)
				.Where(version => version != null)
				.OrderBy(x => x)
				.ToHashSet();
			return results;
		}

		static IReadOnlyDictionary<Component, IReadOnlySet<Version>> releasedNonCoreVersions;

		static async Task<ReleaseNotes> GenerateNotes(IGitHubClient client, Dictionary<Component, Version> forceReleaseVersions = null)
		{
			ReleaseNotes previousNotes = null;
			if (File.Exists("changelog.yml"))
			{
				var existingYml = await File.ReadAllTextAsync("changelog.yml");
				var deserializer = new DeserializerBuilder()
					.Build();

				previousNotes = deserializer.Deserialize<ReleaseNotes>(existingYml);
			}

			var releasesTask = TripleCheckGitHubPagination(
				apiOptions => client.Repository.Release.GetAll(RepoOwner, RepoName, apiOptions),
				release => release.Id);

			var milestones = await TripleCheckGitHubPagination(
				apiOptions => client.Issue.Milestone.GetAllForRepository(RepoOwner, RepoName, new MilestoneRequest {
					State = ItemStateFilter.All
				}, apiOptions),
				milestone => milestone.Id);

			var versionMilestones = milestones
				.Where(milestone => Regex.IsMatch(milestone.Title, @"v[1-9][0-9]*\.[1-9]*[0-9]+\.[1-9]*[0-9]+$"))
				.ToList();

			var releases = await releasesTask;

			var nugetCommonVersions = EnumerateNugetVersions("Tgstation.Server.Common");
			var nugetApiVersions = EnumerateNugetVersions("Tgstation.Server.Api");
			var nugetClientVersions = EnumerateNugetVersions("Tgstation.Server.Client");

			var newDic = new Dictionary<Component, IReadOnlySet<Version>> {
				{ Component.HttpApi, releases
					.Where(x => x.TagName.StartsWith("api-v"))
					.Select(x => Version.Parse(x.TagName[5..]))
					.OrderBy(x => x)
					.ToHashSet() },
				{ Component.DreamMakerApi, releases
					.Where(x => x.TagName.StartsWith("dmapi-v"))
					.Select(x => Version.Parse(x.TagName[7..]))
					.OrderBy(x => x)
					.ToHashSet() },
				{ Component.NugetCommon, await nugetCommonVersions },
				{ Component.NugetApi, await nugetApiVersions },
				{ Component.NugetClient, await nugetClientVersions }
			};

			if (forceReleaseVersions != null)
				foreach (var kvp in forceReleaseVersions)
					if (!newDic[kvp.Key].Any(x => x == kvp.Value))
						newDic[kvp.Key] = newDic[kvp.Key]
							.Concat(new List<Version> { kvp.Value })
							.OrderBy(x => x)
							.ToHashSet();

			releasedNonCoreVersions = newDic;

			var milestonesToProcess = versionMilestones;
			if (previousNotes != null)
			{
				var releasedVersions = previousNotes.Components[Component.Core].Where(cl => !cl.Unreleased).ToList();
				milestonesToProcess = milestonesToProcess
					.Where(x => !releasedVersions.Any(
						version => version.Version == Version.Parse(x.Title.AsSpan(1))))
					.ToList();

				foreach (var kvp in previousNotes.Components)
					if (releasedNonCoreVersions.TryGetValue(kvp.Key, out var releasedComponentVersions))
						kvp.Value.RemoveAll(x => x.Unreleased = !releasedComponentVersions.Any(y => y == x.Version));
					else
						kvp.Value.RemoveAll(x => x.Unreleased);
			}

			var milestonePRTasks = milestonesToProcess
				.Select(milestone => ProcessMilestone(client, milestone))
				.ToList();

			await Task.WhenAll(milestonePRTasks);

			await Task.WhenAll(milestonePRTasks.Select(task => task.Result));

			var coreCls = milestonePRTasks
				.SelectMany(task => task.Result.Result.Components)
				.Where(x => x.Key == Component.Core)
				.ToList();

			DebugAssert(
				coreCls.Count == milestonesToProcess.Count);

			var distinctCoreVersions = coreCls
				.SelectMany(x => x.Value)
				.Select(x => x.Version.ToString())
				.Distinct()
				.Select(Version.Parse)
				.OrderBy(x => x)
				.ToList();

			var missingCoreVersions = milestonesToProcess
				.Where(x => !distinctCoreVersions.Any(y => Version.Parse(x.Title.AsSpan(1)) == y))
				.ToList();

			DebugAssert(missingCoreVersions.Count == 0);

			var changelistsGroupedByComponent =
					milestonePRTasks
						.SelectMany(task => task.Result.Result.Components)
						.GroupBy(kvp => kvp.Key)
						.ToDictionary(grouping => grouping.Key, grouping => grouping.SelectMany(kvp => kvp.Value));

			var releaseNotes = new ReleaseNotes
			{
				Components = new SortedDictionary<Component, List<Changelist>>(
					changelistsGroupedByComponent
						.ToDictionary(
							kvp => kvp.Key,
							kvp => kvp
								.Value
								.GroupBy(changelist => changelist.Version)
								.Select(grouping =>
								{
									var firstEntry = grouping.First();
									return new Changelist
									{
										Changes = grouping.SelectMany(cl => cl.Changes).ToList(),
										ComponentVersions = firstEntry.ComponentVersions,
										Unreleased = firstEntry.Unreleased,
										Version = grouping.Key
									};
								})
								.OrderByDescending(cl => cl.Version)
								.ToList()))
			};

			DebugAssert(releaseNotes.Components.ContainsKey(Component.Core) && releaseNotes.Components[Component.Core].Count == milestonesToProcess.Count);

			if (previousNotes != null)
			{
				foreach (var component in Enum.GetValues<Component>())
				{
					if (!previousNotes.Components.ContainsKey(component))
						continue;

					if (releaseNotes.Components.TryGetValue(component, out var newChangelists))
					{
						var missingVersions = previousNotes.Components[component]
							.Where(olderVersion =>
							{
								var newerVersion = newChangelists.SingleOrDefault(y => olderVersion.Version == y.Version);
								if (newerVersion != null)
								{
									newerVersion.Changes.AddRange(
										olderVersion.Changes.Where(x => !newerVersion.Changes.Any(y => x.PullRequest == y.PullRequest)));
									return false;
								}

								return true;
							});

						releaseNotes.Components[component] = newChangelists
							.Concat(missingVersions)
							.OrderByDescending(cl => cl.Version)
							.ToList();
					}
					else
						releaseNotes.Components[component] = previousNotes.Components[component];
				}
			}

			foreach (var kvp in releaseNotes.Components)
			{
				var distinctCount = kvp.Value.Select(changelist => changelist.Version.ToString()).Distinct().Count();
				DebugAssert(distinctCount == kvp.Value.Count);

				foreach (var cl in kvp.Value)
				{
					cl.DeduplicateChanges();

					if (kvp.Key == Component.Configuration)
						cl.StripConfigVersionMessage();
				}
			}

			return releaseNotes;
		}

		static void PrintChanges(StringBuilder newNotes, Changelist changelist, bool debianMode = false)
		{
			var none = true;
			foreach (var change in changelist.Changes)
				foreach (var line in change.Descriptions)
				{
					none = false;
					newNotes.AppendLine();
					if (debianMode)
						newNotes.Append("  * ");
					else
						newNotes.Append("- ");

					newNotes.Append(line);
					newNotes.Append(" (#");
					newNotes.Append(change.PullRequest);
					newNotes.Append(" @");
					newNotes.Append(change.Author);
					newNotes.Append(')');
				}

			if (debianMode && none)
				throw new Exception($"Changlist {changelist.Version} has no changes!");
		}

		static string GenerateComponentNotes(ReleaseNotes releaseNotes, Component component, Version version, bool useMarkdown)
		{
			var relevantChangelog = releaseNotes.Components[component].FirstOrDefault(x => x.Version == version);

			var newNotes = new StringBuilder(
				useMarkdown
					? "Full changelog can be found [here](https://raw.githubusercontent.com/tgstation/tgstation-server/gh-pages/changelog.yml)."
					: "Full changelog can be found here: https://raw.githubusercontent.com/tgstation/tgstation-server/gh-pages/changelog.yml.");
			if (relevantChangelog != null)
			{
				newNotes.AppendLine();
				PrintChanges(newNotes, relevantChangelog);
			}

			if(component == Component.DreamMakerApi)
			{
				newNotes.AppendLine();
				newNotes.AppendLine("#tgs-dmapi-release");
			}

			var markdown = newNotes.ToString();
			return markdown;
		}

		static async Task<int> ReleaseComponent(IGitHubClient client, Version version, Component component)
		{
			var releaseNotes = await GenerateNotes(client, new Dictionary<Component, Version> { { component, version } });
			await File.WriteAllTextAsync(OutputPath, GenerateComponentNotes(releaseNotes, component, version, true));
			return 0;
		}

		// must run from repo root
		static async Task<int> ReleaseNuget(IGitHubClient client)
		{
			const string PropsPath = "build/Version.props";

			var doc = XDocument.Load(PropsPath);
			var project = doc.Root;
			var xmlNamespace = project.GetDefaultNamespace();
			var versionsPropertyGroup = project.Elements().First(x => x.Name == xmlNamespace + "PropertyGroup");

			var commonVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsCommonLibraryVersion").Value);
			var apiVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsApiLibraryVersion").Value);
			var clientVersion = Version.Parse(versionsPropertyGroup.Element(xmlNamespace + "TgsClientVersion").Value);

			var componentVersions = new Dictionary<Component, Version>
			{
				{ Component.NugetCommon, commonVersion },
				{ Component.NugetApi, apiVersion },
				{ Component.NugetClient, clientVersion },
			};

			var releaseNotes = await GenerateNotes(
				client,
				componentVersions);

			const string CsprojSubstitution = "src/Tgstation.Server.$PROJECT$/Tgstation.Server.$PROJECT$.csproj";
			var csprojNameMap = new Dictionary<Component, string>
			{
				{ Component.NugetCommon, "Common" },
				{ Component.NugetApi, "Api" },
				{ Component.NugetClient, "Client" },
			};

			foreach(var kvp in csprojNameMap)
			{
				var component = kvp.Key;
				var csprojPath = CsprojSubstitution.Replace("$PROJECT$", kvp.Value);

				var markdown = GenerateComponentNotes(releaseNotes, component, componentVersions[component], false);

				var escapedMarkdown = SecurityElement.Escape(markdown);

				var originalCsproj = await File.ReadAllTextAsync(csprojPath);
				var substitutedCsproj = originalCsproj.Replace($"$(TGS_NUGET_RELEASE_NOTES_{kvp.Value.ToUpperInvariant()})", escapedMarkdown);

				await File.WriteAllTextAsync(csprojPath, substitutedCsproj);
			}

			return 0;
		}

		static async Task<int> GenDebianChangelog(IGitHubClient client, Version version, string outputPath, string currentSha)
		{
			var tagsTask = RLR(() => TripleCheckGitHubPagination<RepositoryTag>(
				apiOptions => client.Repository.GetAllTags(RepoOwner, RepoName, apiOptions),
				x => x.Name));
			var currentRefTask = client.Repository.Commit.Get(RepoOwner, RepoName, currentSha);
			var releaseNotes = await GenerateNotes(client);

			// https://www.debian.org/doc/manuals/maint-guide/dreq.en.html#changelog
			// https://www.debian.org/doc/debian-policy/ch-source.html#s-dpkgchangelog

			/*
package (version) distribution(s); urgency=urgency
  [optional blank line(s), stripped]
  * change details
  more change details
  [blank line(s), included in output of dpkg-parsechangelog]
  * even more change details
  [optional blank line(s), stripped]
 -- maintainer name <email address>[two spaces]  date
			*/

			// debian package did not exist before uhhh...
			// var debianPackageFirstRelease = new Version(5, 13, 0);
			// can't use that, there are irreconcilable changelog/version errors
			// keep it straight going forwards
			var noChangelogsBeforeVersion = new Version(5, 14, 0);

			var coreChangelists = releaseNotes
				.Components[Component.Core]
				.Where(x => x.Version >= noChangelogsBeforeVersion && (!x.Unreleased || x.Version == version))
				.OrderByDescending(x => x.Version)
				.ToList();

			var currentReleaseChangelists = new List<SortedDictionary<Component, Changelist>>();

			for (var i = 0; i < coreChangelists.Count; ++i)
			{
				var currentDic = new SortedDictionary<Component, Changelist>();
				currentReleaseChangelists.Add(currentDic);
				var nowRelease = coreChangelists[i];
				var previousRelease = (i + 1) < coreChangelists.Count
					? coreChangelists[i + 1]
					: releaseNotes
						.Components[Component.Core]
						.First(x => x.Version == new Version(5, 13, 7));

				currentDic.Add(Component.Core, nowRelease);
				foreach (var componentKvp in nowRelease.ComponentVersions)
				{
					try
					{
						var component = componentKvp.Key;
						if (component == Component.Core
							|| component == Component.NugetClient
							|| component == Component.NugetApi
							|| component == Component.NugetCommon)
							continue;

						var takeNotesFrom = previousRelease.ComponentVersions[componentKvp.Key];
						var changesEnumerator = releaseNotes
							.Components[component]
							.Where(changelist => changelist.Version > takeNotesFrom && changelist.Version <= componentKvp.Value)
							.SelectMany(x => x.Changes)
							.OrderBy(x => x.PullRequest);
						var changelist = new Changelist
						{
							Version = componentKvp.Value,
							Changes = changesEnumerator
								.ToList(),
						};

						if (changelist.Changes.Any())
							currentDic.Add(component, changelist);
					}
					catch when (Debugger.IsAttached)
					{
						Debugger.Break();
					}
				}
			}

			var builder = new StringBuilder();
			foreach (var releaseDictionary in currentReleaseChangelists)
			{
				var allPrNumbers = releaseDictionary.Values.SelectMany(x => x.Changes.Select(y => y.PullRequest)).Distinct().OrderBy(x => x).ToList();
				var allPrTasks = allPrNumbers
					.Select(x => GetPR(client, x))
					.ToList();

				await Task.WhenAll(allPrTasks);

				var prDict = allPrTasks.ToDictionary(x => x.Result.Number, x => x.Result);

				bool AnyPRHasLabel(string labelName) => prDict.Values.Any(x => x.Labels.Any(y => y.Name == labelName));

				// determine urgency

				string urgency;
				if (AnyPRHasLabel("Priority: CRITICAL"))
					urgency = "critical";
				else if (AnyPRHasLabel("Priority: High"))
					urgency = "high";
				else if (AnyPRHasLabel("Fix"))
					urgency = "medium";
				else
					urgency = "low";

				builder.Append($"tgstation-server (");

				builder.Append(releaseDictionary[Component.Core].Version);
				builder.Append("-1) unstable; urgency=");
				builder.Append(urgency);

				foreach (var kvp in releaseDictionary.Where(x => x.Value.Changes.Count > 0 || x.Key == Component.Configuration))
				{
					builder.AppendLine();
					builder.AppendLine();
					builder.Append("  * The following changes are for ");
					builder.Append(GetComponentDisplayName(kvp.Key, true));
					if(kvp.Key == Component.Configuration)
					{
						builder.Append(". You ");
						if (kvp.Value.Version.Minor == 0 && kvp.Value.Version.Build == 0)
							builder.Append("will need to");
						else
							builder.Append("should");
						builder.Append(" update your `General:ConfigVersion` setting in `/etc/tgstation-server/appsettings.Production.yml` to this new version");
					}

					builder.Append(':');

					PrintChanges(builder, kvp.Value, true);
				}

				builder.AppendLine();
				builder.Append(" -- ");

				GitHubCommit currentRef;
				var tags = await tagsTask;
				var releaseTag = tags.FirstOrDefault(x => x.Name == $"tgstation-server-v{releaseDictionary[Component.Core].Version}");

				if (releaseTag != null)
					currentRef = await client.Repository.Commit.Get(RepoOwner, RepoName, releaseTag.Commit.Sha);
				else
					currentRef = await currentRefTask;

				var committer = currentRef.Commit.Committer;
				if (committer.Name == "GitHub" && committer.Email == "noreply@github.com")
					committer = currentRef.Commit.Author;

				builder.Append(committer.Name);
				builder.Append(" <");
				builder.Append(committer.Email);
				builder.Append(">  ");

				var commitTime = currentRef.Commit.Committer.Date;

				builder.Append(commitTime.ToString("ddd").TrimEnd('.'));
				builder.Append(", ");
				builder.Append(commitTime.ToString("dd"));
				builder.Append(' ');
				builder.Append(commitTime.ToString("MMM").TrimEnd('.'));
				builder.Append(' ');
				builder.AppendLine(commitTime.ToString("yyyy HH:mm:ss zz00"));
			}

			var changelog = builder.ToString().Replace("\r", String.Empty);
			await File.WriteAllTextAsync(outputPath, changelog);
			return 0;
		}

		static async ValueTask GenerateAppCredentials(GitHubClient gitHubClient, string pemBase64)
		{
			var pemBytes = Convert.FromBase64String(pemBase64);
			var pem = Encoding.UTF8.GetString(pemBytes);

			var rsa = RSA.Create();
			rsa.ImportFromPem(pem);

			var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa), SecurityAlgorithms.RsaSha256);
			var jwtSecurityTokenHandler = new JwtSecurityTokenHandler { SetDefaultTimesOnTokenCreation = false };

			var now = DateTime.UtcNow;

			var jwt = jwtSecurityTokenHandler.CreateToken(new SecurityTokenDescriptor
			{
				Issuer = AppId.ToString(),
				Expires = now.AddMinutes(10),
				IssuedAt = now,
				SigningCredentials = signingCredentials
			});

			var jwtStr = jwtSecurityTokenHandler.WriteToken(jwt);

			gitHubClient.Credentials = new Credentials(jwtStr, AuthenticationType.Bearer);

			var installation = await gitHubClient.GitHubApps.GetRepositoryInstallationForCurrent(RepoOwner, RepoName);
			var installToken = await gitHubClient.GitHubApps.CreateInstallationToken(installation.Id);

			gitHubClient.Credentials = new Credentials(installToken.Token);
		}

		static async ValueTask<int> CICompletionCheck(GitHubClient gitHubClient, string currentSha, string pemBase64)
		{
			await GenerateAppCredentials(gitHubClient, pemBase64);

			await gitHubClient.Check.Run.Create(RepoOwner, RepoName, new NewCheckRun("CI Completion", currentSha)
			{
				CompletedAt = DateTime.UtcNow,
				Conclusion = CheckConclusion.Success,
				Output = new NewCheckRunOutput("CI Completion", "The CI Pipeline completed successfully"),
				Status = CheckStatus.Completed,
			});

			return 0;
		}

		static void DebugAssert(bool condition, string message = null)
		{
			// This exists because one of the fucking asserts evaluates an enumerable or something and it was getting optimized out in release
			// I CBA to track this down.
			if (message != null)
				Debug.Assert(condition, message);
			else
				Debug.Assert(condition);
		}
	}
}
