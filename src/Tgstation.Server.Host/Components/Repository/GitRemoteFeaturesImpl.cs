using Microsoft.Extensions.Logging;

using Tgstation.Server.Api.Models;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <summary>
	/// Helper for implementing <see cref="GitRemoteFeaturesBase"/>.
	/// </summary>
	abstract class GitRemoteFeaturesImpl : GitRemoteFeaturesBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="GitRemoteFeaturesImpl"/> class.
		/// </summary>
		/// <param name="logger">The value of <see cref="Logger"/>.</param>
		/// <param name="gitRemoteInformation">The <see cref="Api.Models.GitRemoteInformation"/> for the <see cref="GitRemoteFeaturesBase"/>.</param>
		protected GitRemoteFeaturesImpl(
			ILogger<GitRemoteFeaturesBase> logger,
			GitRemoteInformation gitRemoteInformation) : base(logger, gitRemoteInformation)
		{
		}

		/// <summary>
		/// Override for <see cref="GitRemoteFeaturesBase.GitRemoteInformation"/> that strips nullability.
		/// </summary>
		protected new GitRemoteInformation GitRemoteInformation => base.GitRemoteInformation!;
	}
}
