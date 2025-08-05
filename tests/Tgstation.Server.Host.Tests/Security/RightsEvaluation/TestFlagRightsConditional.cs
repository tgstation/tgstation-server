using System;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using Tgstation.Server.Api.Rights;
using Tgstation.Server.Host.Security.RightsEvaluation;

namespace Tgstation.Server.Host.Tests.Security.RightsEvaluation
{
	[TestClass]
	public sealed class TestFlagRightsConditional
	{
		[TestMethod]
		public void TestOnlyWorksForOneFlag()
		{
			var targetRight = RepositoryRights.ChangeAutoUpdateSettings;
			var conditional = new FlagRightsConditional<RepositoryRights>(targetRight);

			Assert.IsTrue(conditional.Evaluate(targetRight));
			foreach (RepositoryRights right in Enum.GetValues(typeof(RepositoryRights)))
				if (right != targetRight)
				{
					Assert.IsFalse(conditional.Evaluate(right));
					Assert.IsTrue(conditional.Evaluate(targetRight | right));
				}
		}

		[TestMethod]
		public void TestThrowsOnNone()
			=> Assert.Throws<ArgumentOutOfRangeException>(() => _ = new FlagRightsConditional<RepositoryRights>(RepositoryRights.None));

		[TestMethod]
		public void TestThrowsOnMultiBit()
			=> Assert.Throws<ArgumentException>(() => _ = new FlagRightsConditional<RepositoryRights>((RepositoryRights)3));
	}
}
