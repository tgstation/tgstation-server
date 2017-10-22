using System.Runtime.Serialization;

namespace TGServiceInterface
{
	/// <summary>
	/// Information about a pull request
	/// </summary>
	[DataContract]
	public sealed class PullRequestInfo
	{
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
		/// The PR number
		/// </summary>
		[DataMember]
		public int Number { get; private set; }
		/// <summary>
		/// The PR's author
		/// </summary>
		[DataMember]
		public string Author { get; private set; }
		/// <summary>
		/// The PR's title
		/// </summary>
		[DataMember]
		public string Title { get; private set; }
		/// <summary>
		/// The commit the PR was merged locally at
		/// </summary>
		[DataMember]
		public string Sha { get; private set; }
	}
}
