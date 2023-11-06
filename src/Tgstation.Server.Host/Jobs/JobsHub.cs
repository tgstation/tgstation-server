using System;

using Microsoft.AspNetCore.SignalR;

using Tgstation.Server.Api.Hubs;
using Tgstation.Server.Host.Models;
using Tgstation.Server.Host.Security;
using Tgstation.Server.Host.Utils.SignalR;

namespace Tgstation.Server.Host.Jobs
{
	/// <summary>
	/// A SignalR <see cref="Hub"/> for pushing job updates.
	/// </summary>
	sealed class JobsHub : ConnectionMappingHub<JobsHub, IJobsHub>
	{
		/// <summary>
		/// Get the group name for a given <paramref name="instanceId"/>.
		/// </summary>
		/// <param name="instanceId">The <see cref="Instance"/> <see cref="Api.Models.EntityId.Id"/>.</param>
		/// <returns>The name of the group for the <paramref name="instanceId"/>.</returns>
		public static string HubGroupName(long instanceId)
			=> $"instance-{instanceId}";

		/// <summary>
		/// Get the group name for a given <paramref name="job"/>.
		/// </summary>
		/// <param name="job">The <see cref="Job"/>.</param>
		/// <returns>The name of the group for the <paramref name="job"/>.</returns>
		public static string HubGroupName(Job job)
		{
			ArgumentNullException.ThrowIfNull(job);

			if (job.Instance == null)
				throw new InvalidOperationException("job.Instance was null!");

			return HubGroupName(job.Instance.Id.Value);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="JobsHub"/> class.
		/// </summary>
		/// <param name="connectionMapper">The <see cref="IHubConnectionMapper{THub, THubMethods}"/> for the <see cref="ConnectionMappingHub{TChildHub, THubMethods}"/>.</param>
		/// <param name="authenticationContext">The <see cref="IAuthenticationContext"/> for the <see cref="ConnectionMappingHub{TChildHub, THubMethods}"/>.</param>
		public JobsHub(
			IHubConnectionMapper<JobsHub, IJobsHub> connectionMapper,
			IAuthenticationContext authenticationContext)
			: base(connectionMapper, authenticationContext)
		{
		}
	}
}
