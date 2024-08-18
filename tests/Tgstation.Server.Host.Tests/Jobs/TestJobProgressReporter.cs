using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Moq;

namespace Tgstation.Server.Host.Jobs.Tests
{
	[TestClass]
	public sealed class TestJobProgressReporter
	{
		string expectedStageName = null;
		double? expectedProgress = null;
		void Validate(string stageName, double? progress)
		{
			Assert.AreEqual(expectedStageName, stageName);
			Assert.AreEqual(expectedProgress, progress);
		}

		JobProgressReporter Setup()
		{
			expectedStageName = null;
			expectedProgress = 0;
			return new JobProgressReporter(
				Mock.Of<ILogger<JobProgressReporter>>(),
				null,
				Validate);
		}

		[TestMethod]
		public void TestBasicUsage()
		{
			var progressReporter = Setup();

			expectedProgress = 0.4;
			progressReporter.ReportProgress(0.4);
			expectedProgress = 1.0;
			progressReporter.ReportProgress(1.0);
		}

		[TestMethod]
		public void TestNestedUsage()
		{
			var progressReporter = Setup();

			expectedStageName = "Test1";
			var subReporter1 = progressReporter.CreateSection("Test1", 0.5);
			expectedProgress = 0.1;
			subReporter1.ReportProgress(0.2);
			expectedProgress = 0.4;
			subReporter1.ReportProgress(0.8);

			expectedStageName = "Test2";
			var subReporter2 = progressReporter.CreateSection("Test2", 0.5);

			expectedStageName = "Test1";
			expectedProgress = 0.5;
			subReporter1.ReportProgress(1);

			expectedStageName = "Test2";
			expectedProgress = 0.6;
			subReporter2.ReportProgress(0.2);
			expectedProgress = 1.0;
			subReporter2.ReportProgress(1);
		}
	}
}
