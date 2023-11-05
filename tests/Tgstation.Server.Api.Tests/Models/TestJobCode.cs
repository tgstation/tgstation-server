using System;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tgstation.Server.Api.Models.Tests
{
	[TestClass]
	public sealed class TestJobCode
	{
		[TestMethod]
		public void TestAllCodesHaveDescription()
		{
			var jobCodeType = typeof(JobCode);
			foreach (var code in Enum.GetValues(typeof(JobCode)).Cast<JobCode>())
				Assert.IsFalse(
					String.IsNullOrWhiteSpace(
						jobCodeType
							.GetField(code.ToString())
							.GetCustomAttributes(false)
							.OfType<System.ComponentModel.DescriptionAttribute>()
							.FirstOrDefault()
							?.Description),
					$"JobCode {code} is missing a description!");
		}
	}
}
