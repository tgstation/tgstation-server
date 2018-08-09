using System.Collections.Generic;
using Tgstation.Server.Api.Models.Internal;

namespace Tgstation.Server.Host.Components.Watchdog
{
	sealed class InteropInfo
	{
		public string AccessIdentifier { get; set; }

		public bool ApiValidateOnly { get; set; }

		public string InstanceName { get; set; }
		
		public string ChatChannelsJson { get; set; }

		public string ChatCommandsJson { get; set; }

		public string ServerCommandsJson { get; set; }

		public RevisionInformation Revision { get; set; }

		public List<TestMerge> TestMerges { get; } = new List<TestMerge>();
	}
}
