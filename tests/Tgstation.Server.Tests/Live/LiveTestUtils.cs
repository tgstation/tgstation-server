using System;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Tgstation.Server.Tests.Live
{
	static class LiveTestUtils
	{
		public static bool RunningInGitHubActions { get; } = !String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_RUN_ID"));

		public static ILoggerFactory CreateLoggerFactoryForLogger(ILogger logger, out Mock<ILoggerFactory> mockLoggerFactory)
		{
			mockLoggerFactory = new Mock<ILoggerFactory>();
			mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(() =>
			{
				var temp = logger;
				logger = null;

				Assert.IsNotNull(temp);
				return temp;
			})
			.Verifiable();
			return mockLoggerFactory.Object;
		}
	}
}
