using System;

namespace Tgstation.Server.Tests.Live
{
	static class LiveTestUtils
	{
		public static bool RunningInGitHubActions { get; } = !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_RUN_ID"));
	}
}
