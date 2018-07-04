using System;
using System.Collections.Generic;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	sealed class InteropInfo
	{
		public Guid ExecutionId { get; set; }

		public string AccessToken { get; set; }

		public long InstanceId { get; set; }

		public string HostPath { get; set; }

		public bool ApiValidateOnly { get; set; }

		public string InstanceName { get; set; }

		public ushort NextPort { get; set; }

		public string ChatChannelsJson { get; set; }

		public string ChatCommandsJson { get; set; }

		public RevisionInformation Revision { get; set; }

		public List<TestMerge> TestMerges { get; } = new List<TestMerge>();
	}
}
