﻿using Newtonsoft.Json;

namespace TGS.Interface
{
	/// <summary>
	/// Information about a pull request
	/// </summary>
	public sealed class PullRequestInfo
	{
		/// <summary>
		/// The PR number
		/// </summary>
		[JsonProperty]
		public int Number { get; private set; }
		/// <summary>
		/// The PR's author
		/// </summary>
		[JsonProperty]
		public string Author { get; private set; }
		/// <summary>
		/// The PR's title
		/// </summary>
		[JsonProperty]
		public string Title { get; private set; }
		/// <summary>
		/// The commit the PR was merged locally at
		/// </summary>
		[JsonProperty]
		public string Sha { get; private set; }

		/// <summary>
		/// Construct a <see cref="PullRequestInfo"/>
		/// </summary>
		/// <param name="number">The PR number</param>
		/// <param name="author">The PR's author</param>
		/// <param name="title">The PR's title</param>
		/// <param name="sha">The commit the PR was merged locally at</param>
		public PullRequestInfo(int number, string author, string title, string sha)
		{
			Number = number;
			Author = author;
			Title = title;
			Sha = sha;
		}

		/// <summary>
		/// Construct a <see cref="PullRequestInfo"/> for a call to <see cref="Components.ITGRepository.MergedPullRequests"/>
		/// </summary>
		/// <param name="number">The PR number</param>
		/// <param name="sha">The optional commit to merge the PR at</param>
		public PullRequestInfo(int number, string sha = null)
		{
			Number = number;
			Sha = sha;
		}

		/// <summary>
		/// Constructor used by deserializer
		/// </summary>
		[JsonConstructor]
		PullRequestInfo() { }
	}
}
