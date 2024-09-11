using System;
using System.Diagnostics.CodeAnalysis;

using Tgstation.Server.Api.Models;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Utils.GitHub
{
	/// <summary>
	/// Identifies a repository either by its <see cref="RepositoryId"/> or <see cref="Owner"/> and <see cref="Name"/>.
	/// </summary>
	public sealed class RepositoryIdentifier
	{
		/// <summary>
		/// If the <see cref="RepositoryIdentifier"/> is using <see cref="Owner"/> and <see cref="Name"/>.
		/// </summary>
		[MemberNotNullWhen(false, nameof(RepositoryId))]
		[MemberNotNullWhen(true, nameof(Owner))]
		[MemberNotNullWhen(true, nameof(Name))]
		public bool IsSlug => !RepositoryId.HasValue;

		/// <summary>
		/// The repository ID.
		/// </summary>
		public long? RepositoryId { get; }

		/// <summary>
		/// The repository's owning entity.
		/// </summary>
		public string? Owner { get; }

		/// <summary>
		/// The repository's name.
		/// </summary>
		public string? Name { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryIdentifier"/> class.
		/// </summary>
		/// <param name="repositoryId">The value of <see cref="RepositoryId"/>.</param>
		public RepositoryIdentifier(long repositoryId)
		{
			RepositoryId = repositoryId;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryIdentifier"/> class.
		/// </summary>
		/// <param name="owner">The value of <see cref="Owner"/>.</param>
		/// <param name="name">The value of <see cref="Name"/>.</param>
		public RepositoryIdentifier(string owner, string name)
		{
			Owner = owner ?? throw new ArgumentNullException(nameof(owner));
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RepositoryIdentifier"/> class.
		/// </summary>
		/// <param name="gitRemoteInformation">The <see cref="IGitRemoteInformation"/> to build from.</param>
		public RepositoryIdentifier(IGitRemoteInformation gitRemoteInformation)
		{
			ArgumentNullException.ThrowIfNull(gitRemoteInformation);
			if (gitRemoteInformation.RemoteGitProvider == RemoteGitProvider.Unknown)
				throw new ArgumentException("Cannot cast IGitRemoteInformation of Unknown provider to RepositoryIdentifier!", nameof(gitRemoteInformation));

			Owner = gitRemoteInformation.RemoteRepositoryOwner;
			Name = gitRemoteInformation.RemoteRepositoryName;
		}

		/// <inheritdoc />
		public override string ToString()
			=> IsSlug
				? $"{Owner}/{Name}"
				: $"ID {RepositoryId.Value}";
	}
}
