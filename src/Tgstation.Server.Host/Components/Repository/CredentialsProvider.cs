using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using Microsoft.Extensions.Logging;
using System;
using Tgstation.Server.Host.Core;

namespace Tgstation.Server.Host.Components.Repository
{
	/// <inheritdoc />
	sealed class CredentialsProvider : ICredentialsProvider
	{
		/// <summary>
		/// The <see cref="ILogger"/> for the <see cref="CredentialsProvider"/>
		/// </summary>
		readonly ILogger<CredentialsProvider> logger;

		/// <summary>
		/// Construct a <see cref="CredentialsProvider"/>
		/// </summary>
		/// <param name="logger">The value of <see cref="logger"/></param>
		public CredentialsProvider(ILogger<CredentialsProvider> logger)
		{
			this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
		}

		/// <inheritdoc />
		public CredentialsHandler GenerateHandler(string username, string password) => (a, b, supportedCredentialTypes) =>
		{
			var hasCreds = username != null;
			var supportsUserPass = supportedCredentialTypes.HasFlag(SupportedCredentialTypes.UsernamePassword);
			var supportsAnonymous = supportedCredentialTypes.HasFlag(SupportedCredentialTypes.Default);

			logger.LogTrace("Credentials requested. Present: {0}. Supports anonymous: {1}. Supports user/pass: {2}", hasCreds, supportsAnonymous, supportsUserPass);
			if (supportsUserPass)
			{
				if (hasCreds)
					return new UsernamePasswordCredentials
					{
						Username = username,
						Password = password
					};
			}

			if (supportsAnonymous)
				return new DefaultCredentials();

			if (hasCreds)
				throw new JobException("Remote does not support anonymous authentication!");

			throw new JobException("Server does not support anonymous or username/password authentication!");
		};
	}
}
