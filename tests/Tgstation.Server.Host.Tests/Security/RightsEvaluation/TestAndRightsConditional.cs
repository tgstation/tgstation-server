using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Security.RightsEvaluation;

namespace Tgstation.Server.Host.Tests.Security.RightsEvaluation
{
	[TestClass]
	public sealed class TestAndRightsConditional
	{
		[TestMethod]
		public void TestBasicAnding()
		{
			var conditional = new AndRightsConditional<RepositoryRights>(
				new FlagRightsConditional<RepositoryRights>(RepositoryRights.ChangeCredentials),
				new FlagRightsConditional<RepositoryRights>(RepositoryRights.ChangeCommitter));

			foreach (RepositoryRights right in Enum.GetValues(typeof(RepositoryRights)))
				Assert.IsFalse(conditional.Evaluate(right));

			Assert.IsTrue(conditional.Evaluate(RepositoryRights.ChangeCredentials | RepositoryRights.ChangeCommitter));
			Assert.IsTrue(conditional.Evaluate(RepositoryRights.ChangeCredentials | RepositoryRights.ChangeCommitter | RepositoryRights.SetReference));
		}

		[TestMethod]
		public void TestThrows()
		{
			Assert.Throws<ArgumentNullException>(() => _ = new AndRightsConditional<RepositoryRights>(
				null,
				null));
			Assert.Throws<ArgumentNullException>(() => _ = new AndRightsConditional<RepositoryRights>(
				new FlagRightsConditional<RepositoryRights>(RepositoryRights.SetOrigin),
				null));
		}
	}
}
