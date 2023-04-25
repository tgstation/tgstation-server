using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Tgstation.Server.Api.Rights.Tests
{
	/// <summary>
	/// Tests for <see cref="Rights"/>
	/// </summary>
	[TestClass]
	public sealed class TestRights
	{
		[TestMethod]
		public void TestAllPowerOfTwo()
		{
			var ulongType = typeof(ulong);
			foreach (var I in Enum.GetValues(typeof(RightsType)).Cast<RightsType>())
			{
				var expectedLog = -1;
				var rightType = RightsHelper.RightToType(I);
				foreach (var J in Enum.GetValues(rightType))
				{
					Assert.AreEqual(ulongType, Enum.GetUnderlyingType(rightType));
					var asUlong = (ulong)J;
					var isOne = asUlong == 1;
					if (!isOne)
						Assert.AreEqual(0U, asUlong % 2, String.Format("Enum {0} of {1} is not a power of 2!", Enum.GetName(rightType, asUlong), rightType));

					if (expectedLog > -1)
					{
						var log = Math.Log(asUlong, 2);
						if (log != expectedLog)
							Assert.Fail(String.Format("Expected Log2({1}) == {0} to come next for {2}, got {3} instead!", expectedLog, Enum.GetName(rightType, asUlong), rightType, log));
					}
					++expectedLog;
				}
			}
		}

		[TestMethod]
		public void TestAllRightsWorks()
		{
			var allByondRights = ByondRights.CancelInstall | ByondRights.InstallOfficialOrChangeActiveVersion | ByondRights.ListInstalled | ByondRights.ReadActive | ByondRights.InstallCustomVersion | ByondRights.DeleteInstall;
			var automaticByondRights = RightsHelper.AllRights<ByondRights>();

			Assert.AreEqual(allByondRights, automaticByondRights);
		}
	}
}
