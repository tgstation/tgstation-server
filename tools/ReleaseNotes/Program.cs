using Octokit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReleaseNotes
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Missing version argument!");
                return 1;
            }

            var versionString = args[0];
            if (!Version.TryParse(versionString, out var version))
            {
                Console.WriteLine("Invalid version: " + versionString);
                return 2;
            }

            var doNotCloseMilestone = args.Length >= 2 && args[1].ToUpperInvariant() == "--NO-CLOSE";

            const string ReleaseNotesEnvVar = "TGS_RELEASE_NOTES_TOKEN";
            var githubToken = Environment.GetEnvironmentVariable(ReleaseNotesEnvVar);
            if (String.IsNullOrWhiteSpace(githubToken))
            {
                Console.WriteLine("Missing " + ReleaseNotesEnvVar + " environment variable!");
                return 3;
            }

            try
            {
                var client = new GitHubClient(new ProductHeaderValue("tgs_release_notes"));
                client.Credentials = new Credentials(githubToken);

                const string RepoOwner = "tgstation";
                const string RepoName = "tgstation-server";

                var releasesTask = client.Repository.Release.GetAll(RepoOwner, RepoName);

                Console.WriteLine("Getting pull requests in milestone " + versionString + "...");
                var milestonePRs = await client.Search.SearchIssues(new SearchIssuesRequest
                {
                    Milestone = versionString,
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
                var releaseDictionary = new Dictionary<int, List<string>>();
                var authorizedUsers = new Dictionary<long, Task<bool>>();

                async Task GetReleaseNotesFromPR(Issue pullRequest)
                {
                    PullRequest fullPR = null;
                    async Task GetFullPR()
                    {
                        if (fullPR != null)
                            return;
                        fullPR = await client.Repository.PullRequest.Get(RepoOwner, RepoName, pullRequest.Number).ConfigureAwait(false);
                    };

                    async Task<Milestone> GetMilestone()
                    {
                        await GetFullPR().ConfigureAwait(false);
                        if (fullPR.Milestone == null)
                            return null;
                        return await client.Issue.Milestone.Get(RepoOwner, RepoName, fullPR.Milestone.Number);
                    };

                    if (pullRequest.State.Value == ItemState.Closed)
                    {
                        //need to check it was merged
                        lock (milestoneTaskLock)
                            if (milestoneTask == null)
                                milestoneTask = GetMilestone();
                        if (!fullPR.Merged)
                            return;
                    };

                    async Task BuildNotesFromComment(string comment, User user)
                    {
                        var commentSplits = comment.Split('\n');
                        var notesOpen = false;
                        var notesClosed = false;
                        var notes = new List<string>();
                        foreach (var line in commentSplits)
                        {
                            var trimmedLine = line.Trim();
                            if (!notesOpen)
                            {
                                notesOpen = trimmedLine.StartsWith(":cl:", StringComparison.Ordinal);
                                notesClosed = false;
                                continue;
                            }
                            if (trimmedLine.StartsWith("/:cl:", StringComparison.Ordinal))
                            {
                                notesClosed = true;
                                notesOpen = false;
                                continue;
                            }
                            if (trimmedLine.Length == 0)
                                continue;
                            notes.Add(trimmedLine);
                        }
                        if (!notesClosed || notes.Count == 0)
                            return;

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
                                var perm = await client.Repository.Collaborator.ReviewPermission(RepoOwner, RepoName, user.Login).ConfigureAwait(false);
                                ourTcs.SetResult(perm.Permission == PermissionLevel.Write || perm.Permission == PermissionLevel.Admin);
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
                                Console.WriteLine("#" + pullRequest.Number + " - " + I + " (@" + user.Login + ")");
                            if (releaseDictionary.TryGetValue(pullRequest.Number, out var currentValues))
                                currentValues.AddRange(notes);
                            else
                                releaseDictionary.Add(pullRequest.Number, notes);
                        }
                    }

                    var comments = await client.Issue.Comment.GetAllForIssue(RepoOwner, RepoName, pullRequest.Number).ConfigureAwait(false);
                    await Task.WhenAll(BuildNotesFromComment(pullRequest.Body, pullRequest.User), Task.WhenAll(comments.Select(x => BuildNotesFromComment(x.Body, x.User)))).ConfigureAwait(false);

                    lock (milestoneTaskLock)
                        if (milestoneTask == null)
                            milestoneTask = GetMilestone();
                }

                var tasks = new List<Task>();
                foreach (var I in milestonePRs.Items)
                    tasks.Add(GetReleaseNotesFromPR(I));

                var releases = await releasesTask.ConfigureAwait(false);

                var releasingSuite = version.Major;

                Version highestReleaseVersion = null;
                Release highestRelease = null;
                foreach (var I in releases)
                {
                    if (!Version.TryParse(I.TagName.Replace("tgstation-server-v", String.Empty), out var currentReleaseVersion))
                    {
                        Console.WriteLine("WARNING: Unable to determine version of release " + I.HtmlUrl);
                        continue;
                    }

                    if (currentReleaseVersion.Major == releasingSuite && (highestReleaseVersion == null || currentReleaseVersion > highestReleaseVersion))
                    {
                        highestReleaseVersion = currentReleaseVersion;
                        highestRelease = I;
                    }
                }

                if (highestReleaseVersion == null)
                {
                    Console.WriteLine("Unable to determine highest release version for suite " + releasingSuite + "!");
                    return 6;
                }

                var oldNotes = highestRelease.Body;

                var splits = new List<string>(oldNotes.Split('\n'));
                //trim away all the lines that don't start with #

                string keepThisRelease;
                if (version.Revision == 0)
                    if (version.Build == 0)
                        keepThisRelease = "# ";
                    else
                        keepThisRelease = "## ";
                else
                    keepThisRelease = "### ";

                for (; !splits[0].StartsWith(keepThisRelease, StringComparison.Ordinal); splits.RemoveAt(0))
                    if (splits.Count == 1)
                    {
                        Console.WriteLine("Error formatting release notes: Can't detemine notes start!");
                        return 7;
                    }

                oldNotes = String.Join('\n', splits);

                string prefix;
                switch (releasingSuite)
                {
                    case 3:
                        prefix = "The /tg/station server suite";
                        break;
                    default:
                        prefix = "See https://tgstation.github.io/tgstation-server for installation instructions";
                        break;
                }

                var newNotes = new StringBuilder(prefix);
                newNotes.Append(Environment.NewLine);
                newNotes.Append(Environment.NewLine);
                if (version.Revision == 0)
                    if (version.Build == 0)
                    {
                        newNotes.Append("# [Version ");
                        newNotes.Append(version.Minor);
                    }
                    else
                    {
                        newNotes.Append("## [Changelog for ");
                        newNotes.Append(version.Build);
                        newNotes.Append(".x");
                    }
                else
                {
                    newNotes.Append("### [Patch ");
                    newNotes.Append(version.Revision);
                }
                newNotes.Append("](");
                var milestone = await milestoneTask.ConfigureAwait(false);
                if (milestone == null)
                {
                    Console.WriteLine("Unable to detemine milestone!");
                    return 9;
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

                foreach (var I in releaseDictionary)
                    foreach (var note in I.Value)
                    {
                        newNotes.Append(Environment.NewLine);
                        newNotes.Append("- ");
                        newNotes.Append(note);
                        newNotes.Append(" (#");
                        newNotes.Append(I.Key);
                        newNotes.Append(')');
                    }

                newNotes.Append(Environment.NewLine);
                newNotes.Append(Environment.NewLine);
                newNotes.Append(oldNotes);

                Console.WriteLine("Writing out new release notes...");
                var releaseNotes = newNotes.ToString();
                await File.WriteAllTextAsync("release_notes.md", releaseNotes).ConfigureAwait(false);

                if (doNotCloseMilestone)
                    Console.WriteLine("Not closing milestone due to parameter!");
                else
                {
                    Console.WriteLine("Closing milestone...");
                    await client.Issue.Milestone.Update(RepoOwner, RepoName, milestone.Number, new MilestoneUpdate
                    {
                        State = ItemState.Closed
                    }).ConfigureAwait(false);
                }

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
