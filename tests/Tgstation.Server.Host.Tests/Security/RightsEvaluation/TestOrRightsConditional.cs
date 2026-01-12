using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Security.RightsEvaluation;

namespace Tgstation.Server.Host.Tests.Security.RightsEvaluation
{
	[TestClass]
	public sealed class TestOrRightsConditional
	{
		[TestMethod]
		public void TestBasicOring()
		{
			var conditional = new OrRightsConditional<RepositoryRights>(
				new FlagRightsConditional<RepositoryRights>(RepositoryRights.ChangeCredentials),
				new FlagRightsConditional<RepositoryRights>(RepositoryRights.ChangeCommitter));

			foreach (RepositoryRights right in Enum.GetValues(typeof(RepositoryRights)))
				if (right != RepositoryRights.ChangeCredentials && right != RepositoryRights.ChangeCommitter)
					Assert.IsFalse(conditional.Evaluate(right));

			Assert.IsTrue(conditional.Evaluate(RepositoryRights.ChangeCredentials | RepositoryRights.ChangeCommitter));
			Assert.IsTrue(conditional.Evaluate(RepositoryRights.ChangeCredentials));
			Assert.IsTrue(conditional.Evaluate(RepositoryRights.ChangeCommitter));
			Assert.IsTrue(conditional.Evaluate(RepositoryRights.ChangeCommitter | RepositoryRights.MergePullRequest));
			Assert.IsTrue(conditional.Evaluate(RepositoryRights.ChangeCredentials | RepositoryRights.ChangeCommitter | RepositoryRights.MergePullRequest));
		}

		[TestMethod]
		public void TestThrows()
		{
			Assert.Throws<ArgumentNullException>(() => _ = new OrRightsConditional<RepositoryRights>(
				null,
				null));
			Assert.Throws<ArgumentNullException>(() => _ = new OrRightsConditional<RepositoryRights>(
				new FlagRightsConditional<RepositoryRights>(RepositoryRights.SetOrigin),
				null));
		}
	}
}
