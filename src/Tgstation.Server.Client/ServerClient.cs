using System;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Tgstation.Server.Api;
using Tgstation.Server.Api.Models;
using Tgstation.Server.Client.Components;

namespace Tgstation.Server.Client
{
	/// <inheritdoc />
	sealed class ServerClient : IServerClient
	{
		/// <inheritdoc />
		public Token Token { get; }

		/// <inheritdoc />
		public TimeSpan Timeout
		{
			get => apiClient.Timeout;
			set => apiClient.Timeout = value;
		}

		/// <inheritdoc />
		public IInstanceManagerClient Instances { get; }

		/// <inheritdoc />
		public IAdministrationClient Administration { get; }

		/// <inheritdoc />
		public IUsersClient Users { get; }
		
		readonly IApiClient apiClient;

		public ServerClient(IApiClient apiClient, Token token)
		{
			this.apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
			Token = token ?? throw new ArgumentNullException(nameof(token));

			if (Token.Bearer != apiClient.Headers.Token)
				throw new ArgumentOutOfRangeException(nameof(token), token, "Provided token does not match apiClient headers!");

			Administration = new AdministrationClient(apiClient);
		}

		/// <inheritdoc />
		public void Dispose() => apiClient.Dispose();

		/// <inheritdoc />
		public async Task<Job> CreateTaskFromJob(Job job, TimeSpan requeryRate, CancellationToken cancellationToken)
		{
			if (job == null)
				throw new ArgumentNullException(nameof(job));

			var jobsClient = Instances.CreateClient(job.Instance).Jobs;

			while (!job.StoppedAt.HasValue)
			{
				await Task.Delay(requeryRate, cancellationToken).ConfigureAwait(false);
				job = await jobsClient.Read(job, cancellationToken).ConfigureAwait(false);
			}
			return job;
		}

		/// <inheritdoc />
		public Task<Version> Version(CancellationToken cancellationToken) => apiClient.Read<Version>(Routes.Root, cancellationToken);
	}
}